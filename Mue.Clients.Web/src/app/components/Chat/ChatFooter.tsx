import { FunctionalComponent } from "preact";
import { useState } from "preact/hooks";
import { Input } from "reactstrap";

interface CommandInputProps {
    submitCommand: (command: string) => Promise<boolean>;
}

const CommandInput: FunctionalComponent<CommandInputProps> = ({
    submitCommand,
}) => {
    const [submitHistory, setSubmitHistory] = useState<string[]>([]);
    const [inputText, setInputText] = useState<string>("");

    const submit = async () => {
        if (await submitCommand(inputText)) {
            setInputText("");
            setSubmitHistory((prev) => [...prev, inputText]);
        }
    };

    return (
        <Input
            placeholder="Command"
            value={inputText}
            onChange={(e) =>
                setInputText((e.currentTarget as HTMLInputElement).value)
            }
            onKeyDown={(e) => {
                if (e.key == "ArrowUp" && !inputText) {
                    e.preventDefault();
                    if (submitHistory.length > 0) {
                        setInputText(submitHistory[submitHistory.length - 1]);
                    }
                } else if (e.key == "Enter") {
                    e.preventDefault();
                    submit();
                }
            }}
        />
    );
};

interface ChatFooterProps extends CommandInputProps {
    available: boolean;
}

export const ChatFooter: FunctionalComponent<ChatFooterProps> = ({
    available,
    submitCommand,
}) => {
    if (!available) return null;

    return (
        <footer className="fixed-bottom">
            <CommandInput submitCommand={submitCommand} />
        </footer>
    );
};
