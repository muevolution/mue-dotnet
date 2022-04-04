using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace Mue.Scripting
{
    public class PythonScriptEngine : IScriptEngine
    {
        public string? ScriptName { get; private set; }
        public string? ScriptText { get; private set; }
        public uint ScriptTimeout { get; private set; } = 100; // minimum unset
        private dynamic? _binding;
        private Thread? _scriptEvalThread;
        private Timer? _scriptEvalTimer;
        private TaskCompletionSource? _taskCompletionSource;

        public PythonScriptEngine() { }

        public Task SpawnAndRun(string scriptName, string script, uint timeout, dynamic binding)
        {
            this.ScriptName = scriptName;
            this.ScriptText = script;
            this.ScriptTimeout = timeout;
            this._binding = binding;

            _taskCompletionSource = new TaskCompletionSource();

            _scriptEvalThread = new Thread(this.Evaluate);
            _scriptEvalThread.Start();

            return _taskCompletionSource.Task;
        }

        private void Evaluate()
        {
            try
            {
                _scriptEvalTimer = new Timer(this.TimeoutCallback, null, this.ScriptTimeout, Timeout.Infinite);

                var engine = Python.CreateEngine(new Dictionary<string, object> {
                    {"NoSite", true},
                    {"NoUserSite", true},
                    {"Debug", false},
                    {"Tracing", false}
                });

                // Restrict the import path
                var paths = new List<string>();
                paths.Add(System.IO.Path.Join(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PyLib"));
                engine.SetSearchPaths(paths);

                // Generate an instance of MueBinder
                var binderClass = BuildBinderClassInstance(engine, _binding);

                // Execute the user script
                var scope = engine.CreateScope();

                try
                {
                    // Compile the user script
                    var scriptSource = engine.CreateScriptSourceFromString(ScriptText, ScriptName, Microsoft.Scripting.SourceCodeKind.File);

                    var compiled = scriptSource.Compile();
                    compiled.Execute(scope);
                }
                catch (Microsoft.Scripting.SyntaxErrorException e)
                {
                    throw new MueScriptInvalidSyntax(e.Line, e.Column, e);
                }
                catch (Exception e)
                {
                    throw new MueScriptException("An error occured while compiling a script.", e);
                }

                // Call the user script main function
                var entrypoint = scope.GetVariable("__mue_entry__");
                if (entrypoint == null)
                {
                    throw new MueScriptEntryMissing();
                }

                try
                {
                    engine.Operations.Invoke(entrypoint, binderClass);
                }
                catch (Exception e)
                {
                    var ex = new MueScriptException("An error occured while running a script.", e);
                    ex.SetPythonStack(engine);
                    throw ex;
                }

                _taskCompletionSource?.TrySetResult();
            }
            catch (Exception e)
            {
                _taskCompletionSource?.TrySetException(e);
            }
            finally
            {
                _scriptEvalTimer?.Dispose();
            }
        }

        private void TimeoutCallback(object? state)
        {
            _scriptEvalTimer?.Dispose();
            _scriptEvalThread?.Interrupt();
            _taskCompletionSource?.TrySetCanceled();
        }

        private static dynamic BuildBinderClassInstance(ScriptEngine engine, dynamic binding)
        {
            var binderScope = engine.CreateScope();
            binderScope.SetVariable("binding", binding);
            var binderScriptSource = engine.CreateScriptSourceFromString(@"
from mue_engine import MueBinding
binder = MueBinding(binding)");
            var binderScriptCompiled = binderScriptSource.Compile();
            try
            {
                binderScriptCompiled.Execute(binderScope);
            }
            catch (Microsoft.Scripting.SyntaxErrorException e)
            {
                throw new MueScriptInvalidSyntax(e.Line, e.Column, e);
            }
            var binderClass = binderScope.GetVariable("binder");
            return binderClass;
        }
    }
}
