export type HubEventTypes = "welcome" | "message" | "echo" | "disconnect" | "fatal";

export interface OperationResponse {
    success: boolean;
    fatal: boolean;
    message: string;
    code: MueCodes;
}

export interface AuthRequest {
    username: string;
    password: string;
    is_registration: boolean;
}

export interface CommandRequest {
    command: string;
    is_expanded?: boolean;
    params?: { [key: string]: string };
}

export interface CommunicationsMessage {
    source: string;
    target: string;
    message: string;
    extended_content?: { [key: string]: string };
    extended_format?: string;
    meta?: { [key: string]: string };
}

export enum MueCodes {
    Nothing = 0,
    Success = 1,
    UnknownError = 2,
    LoginError = 100,
    UnauthenticatedError = 101,
    PubSubError = 201,
};
