import { FunctionalComponent } from "preact";
import { useState } from "preact/hooks";
import { Button, Input } from "reactstrap";

interface AuthFormProps {
    authenticate(username: string, password: string): Promise<void>;
    register(username: string, password: string): Promise<void>;
}

export const AuthForm: FunctionalComponent<AuthFormProps> = ({
    authenticate,
    register,
}) => {
    const [username, setUsername] = useState("Kauko");
    const [password, setPassword] = useState("kaukopasswd");

    return (
        <div>
            <Input
                value={username}
                onChange={(e) =>
                    setUsername((e.currentTarget as HTMLInputElement).value)
                }
            />
            <Input
                type="password"
                value={password}
                onChange={(e) =>
                    setPassword((e.currentTarget as HTMLInputElement).value)
                }
            />
            <Button onClick={() => authenticate(username, password)}>
                Login
            </Button>
            <Button onClick={() => register(username, password)}>
                Register
            </Button>
        </div>
    );
};
