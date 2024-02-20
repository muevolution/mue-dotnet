import { useEffect, useState } from "preact/hooks";
import { HubClientCallbacks } from "../mue/HubClient";
import { HubClientSession } from "../mue/HubClientSession";

export function useMueServerConnection(
    url: string,
    callbacks: HubClientCallbacks,
) {
    const [error, setError] = useState<Error>();
    const [session] = useState<HubClientSession>(
        () => new HubClientSession(url, callbacks),
    );

    useEffect(() => {
        session
            .open()
            .then(() => {
                console.log("Connection opened");
            })
            .catch((err) => {
                console.error("Session open got error", err);
                setError(err);
            });

        return () => {
            session.close();
            setError(undefined);
        };
    }, [session]);

    return {
        client: session.client,
        close: () => session.close(),
        isAuthenticated: session.isAuthenticated,
        error,
    };
}
