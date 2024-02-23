import { useEffect, useState } from "preact/hooks";

interface ServerConfig {
    hubUrl: string;
}

async function getServerConfig() {
    const req = await fetch("server.json");
    return (await req.json()) as ServerConfig;
}

export function useServerConfig() {
    const [serverConfig, setServerConfig] = useState<ServerConfig>();
    const [lastError, setLastError] = useState<Error>();

    useEffect(() => {
        if (!serverConfig) {
            getServerConfig()
                .then((c) => setServerConfig(c))
                .catch((err) => {
                    console.error("Failed to fetch server config", err);
                    setLastError(err);
                });
        }
    });

    return { config: serverConfig, error: lastError };
}
