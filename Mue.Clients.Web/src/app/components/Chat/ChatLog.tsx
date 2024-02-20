import { useEffect, useRef } from "preact/hooks";
import { DisplayMessage, MessageContents } from "../../mue/Message";
import * as css from "./ChatLog.module.scss";
import type { FunctionalComponent } from "preact";
import { Table } from "reactstrap";

const MessageText: FunctionalComponent<MessageRowProps> = ({ message }) => {
    return <pre title={JSON.stringify(message.data)}>{message.message}</pre>;
};

interface MessageDetailsProps {
    messageData: MessageContents;
}

type TableContent = {
    message: string;
    table: string[][];
    has_header: boolean;
};

const MessageTable: FunctionalComponent<MessageDetailsProps> = ({
    messageData,
}) => {
    if (
        !("message" in messageData) ||
        !messageData.message.meta?.table_content
    ) {
        return (
            <pre title={JSON.stringify(messageData)}>
                (Unable to render message)
            </pre>
        );
    }

    const rawTableData = messageData.message.meta?.table_content;
    const tableData = JSON.parse(rawTableData) as TableContent;
    const headerRow = tableData.has_header && tableData.table[0];
    const bodyRows = tableData.has_header
        ? tableData.table.slice(1)
        : tableData.table;

    const renderRow = (row: string[], useTh: boolean = false) => (
        <tr>
            {row.map((col, ci) => (
                <>{useTh ? <th key={ci}>{col}</th> : <td key={ci}>{col}</td>}</>
            ))}
        </tr>
    );

    return (
        <div title={JSON.stringify(messageData)}>
            {tableData.message && (
                <pre className={css.tableMessage}>{tableData.message}</pre>
            )}
            <Table>
                {headerRow && <thead>{renderRow(headerRow, true)}</thead>}
                <tbody>{bodyRows.map((r) => renderRow(r))}</tbody>
            </Table>
        </div>
    );
};

interface MessageRowProps {
    message: DisplayMessage;
}

const MessageRow: FunctionalComponent<MessageRowProps> = ({ message }) => {
    const messageRenderer =
        message.data &&
        "message" in message.data &&
        message.data.message.meta?.message_renderer;

    return (
        <div>
            <hr />
            <div className="small">{message.when.toTimeString()}</div>
            {messageRenderer === "table" ? (
                <MessageTable messageData={message.data!} />
            ) : (
                <MessageText message={message} />
            )}
        </div>
    );
};

interface ChatLogProps {
    messages: DisplayMessage[];
}

export const ChatLog: FunctionalComponent<ChatLogProps> = ({ messages }) => {
    const logDivRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (logDivRef.current) {
            logDivRef.current.scrollIntoView({ behavior: "smooth" });
        }
    }, [messages]);

    return (
        <div className={css.chatlog}>
            {messages.map((m, i) => (
                <MessageRow message={m} key={i} />
            ))}
            <div
                id="chat-bottom"
                ref={logDivRef}
                className={css["chat-bottom"]}
            ></div>
        </div>
    );
};
