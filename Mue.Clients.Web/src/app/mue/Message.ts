import { CommunicationsMessage, OperationResponse } from "./consts";

type MessageMessageData = {
    message: CommunicationsMessage;
    code: number;
}
type MessageResponseData = {
    res: OperationResponse;
}
export type MessageContents = MessageMessageData | MessageResponseData;

export interface DisplayMessage {
    message: string;
    data?: MessageContents;
    when: Date;
}
