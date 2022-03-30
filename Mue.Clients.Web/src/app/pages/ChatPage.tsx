import { useState } from "preact/hooks";
import { AuthForm } from "../components/AuthForm";
import { ChatFooter } from "../components/Chat/ChatFooter";
import { ChatLog } from "../components/Chat/ChatLog";
import { useMueServerConnection } from "../components/MueServerConnection";
import { CommunicationsMessage, MueCodes } from "../mue/consts";
import { HubClientCallbacks } from "../mue/HubClient";
import { DisplayMessage, MessageContents } from "../mue/Message";

const ChatPage: React.FC = () => {
    const [messageLog, setMessageLog] = useState<DisplayMessage[]>([]);

    const appendMessageLog = (message: string, data?: MessageContents) => {
        setMessageLog((val) => {
            const top = val.slice(-49);
            top.push({ message, data, when: new Date() });
            return top;
        });
    };

    const callbacks: HubClientCallbacks = {
        onWelcome: (motd: string) => appendMessageLog("Welcome! MOTD: " + motd),
        onMessage: (message: CommunicationsMessage, code: number) => appendMessageLog(message.message, { message, code }),
        onEcho: (message: string) => appendMessageLog("Echo: " + message),
        onDisconnect: (reason?: string) => {
            appendMessageLog("Disconnected. Reason: " + reason);
            close();
        },
        onFatal: (message: string) => {
            appendMessageLog("Fatal error. " + message);
            close();
        },
        _internal: (message: string) => {
            appendMessageLog("Client message: " + message);
        },
    };

    const { client, close, error, isAuthenticated } = useMueServerConnection("http://localhost:5000/mueclient", callbacks);

    if (error) {
        return <p>Error! <pre>{error.message || JSON.stringify(error.message)}</pre></p>
    }
    if (!client) {
        return <p>Error! No client!</p>
    }

    const authenticate = async (username: string, password: string) => {
        try {
            const res = await client.auth({ username, password, is_registration: false });
            if (res.code != MueCodes.Success) {
                appendMessageLog("Error while logging in: " + res.message, { res });
            } else {
                appendMessageLog(res.message);
            }
        } catch (err: unknown) {
            appendMessageLog("Error calling client: " + (err as any).message);
        }
    };

    const register = async (username: string, password: string) => {
        try {
            const res = await client.auth({ username, password, is_registration: true });
            if (res.code != MueCodes.Success) {
                appendMessageLog("Error while registering: " + res.message, { res });
            } else {
                appendMessageLog(res.message);
            }
        } catch (err: unknown) {
            appendMessageLog("Error calling client: " + (err as any).message);
        }
    };

    const submitCommand = async (command: string) => {
        try {
            const res = await client.command({ command });
            if (res.code !== MueCodes.Success) {
                appendMessageLog("Error sending command: " + res.message, { res });
                return false;
            }

            return true;
        } catch (err: unknown) {
            appendMessageLog("Error calling client: " + (err as any).message);
            return false;
        }
    }

    return <>
        <div className="container">
            <ChatLog messages={messageLog} />

            {!isAuthenticated && <AuthForm authenticate={authenticate} register={register} />}
        </div>

        <ChatFooter available={isAuthenticated} submitCommand={submitCommand} />
    </>;
};

export default ChatPage;
