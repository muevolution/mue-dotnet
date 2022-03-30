import { useState } from "preact/hooks";
import { Form } from "react-bootstrap";

interface CommandInputProps {
    submitCommand: (command: string) => Promise<boolean>;
}

const CommandInput: React.FC<CommandInputProps> = ({ submitCommand }) => {
    const [inputText, setInputText] = useState<string>("");

    const submit = async () => {
        if (await submitCommand(inputText)) {
            setInputText("");
        }
    };

    return <Form.Control
        type="text"
        placeholder="Command"
        value={inputText}
        onChange={(e) => setInputText(e.target.value)}
        onKeyPress={(e) => {
            if (e.charCode == 13) {
                e.preventDefault();
                submit();
            }
        }}
    />
};

interface ChatFooterProps extends CommandInputProps {
    available: boolean;
}

export const ChatFooter: React.FC<ChatFooterProps> = ({ available, submitCommand }) => {
    if (!available) return null;

    return <footer className="fixed-bottom">
        <CommandInput submitCommand={submitCommand} />
    </footer>;
}
