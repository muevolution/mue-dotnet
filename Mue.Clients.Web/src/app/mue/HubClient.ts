import * as signalR from "@microsoft/signalr";
import {
    AuthRequest,
    CommandRequest,
    CommunicationsMessage,
    MueCodes,
    OperationResponse,
} from "./consts";

export class HubClient {
    private _isAuthenticated: boolean = false;
    private _cachedAuth?: AuthRequest;

    constructor(
        protected connection: signalR.HubConnection,
        callbacks: HubClientCallbacks,
    ) {
        connection.on("Welcome", (motd: string) => {
            if (this._cachedAuth) {
                this.auth(this._cachedAuth);
                callbacks._internal("Reconnected to server");
            } else {
                this._isAuthenticated = false;
                callbacks.onWelcome(motd);
            }
        });
        connection.on("Message", callbacks.onMessage);
        connection.on("Echo", callbacks.onEcho);
        connection.on("Disconnect", callbacks.onDisconnect);
        connection.on("Fatal", callbacks.onFatal);
    }

    get isAuthenticated() {
        return this._isAuthenticated;
    }

    // client to server
    auth = async (data: AuthRequest) => {
        const res = await this.connection.invoke<OperationResponse>(
            "Auth",
            data,
        );

        if (res.code === MueCodes.Success) {
            this._isAuthenticated = true;
            this._cachedAuth = data;
        } else {
            this._isAuthenticated = false;
        }

        return res;
    };
    command = (request: CommandRequest) =>
        this.connection.invoke<OperationResponse>("Command", request);
    echo = (message: string) =>
        this.connection.invoke<OperationResponse>("Echo", message);
    disconnect = () => this.connection.invoke<OperationResponse>("Disconnect");
}

export interface HubClientCallbacks {
    onWelcome: (motd: string) => void;
    onMessage: (message: CommunicationsMessage, code: number) => void;
    onEcho: (message: string) => void;
    onDisconnect: (reason?: string) => void;
    onFatal: (message: string, code: number) => void;
    _internal: (message: string) => void;
}
