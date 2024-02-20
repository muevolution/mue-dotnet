import * as signalR from "@microsoft/signalr";
import { HubClient, HubClientCallbacks } from "./HubClient";

export class HubClientSession {
    private _connection?: signalR.HubConnection;
    private _client?: HubClient;

    constructor(
        private url: string,
        private callbacks: HubClientCallbacks,
    ) {}

    get client() {
        return this._client;
    }

    get isAuthenticated() {
        return this._client?.isAuthenticated || false;
    }

    async open() {
        this._connection = new signalR.HubConnectionBuilder()
            .withUrl(this.url)
            .withAutomaticReconnect()
            .build();

        this._connection.onclose((err) => {
            this._client = undefined;
            console.log("Connection closed by server", err);
        });

        await this._connection.start();

        this._client = new HubClient(this._connection, this.callbacks);
    }

    async close() {
        if (this._connection) {
            await this._connection.stop();
            console.log("Connection closed by user");
        }
    }
}
