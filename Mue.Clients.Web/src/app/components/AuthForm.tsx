import { useState } from "preact/hooks";
import { Button, Form } from "react-bootstrap";

interface AuthFormProps {
    authenticate(username: string, password: string): Promise<void>;
    register(username: string, password: string): Promise<void>;
}

export const AuthForm: React.FC<AuthFormProps> = ({ authenticate, register }) => {
    const [username, setUsername] = useState("Kauko");
    const [password, setPassword] = useState("kaukopasswd");

    return <div>
        <Form.Control type="text" value={username} onChange={(e) => setUsername(e.target.value)} />
        <Form.Control type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <Button onClick={() => authenticate(username, password)}>Login</Button>
        <Button onClick={() => register(username, password)}>Register</Button>
    </div>;
};
