import { Table } from "react-bootstrap";

const MessageContainer = ({ messages }) => {
  return (
    <div>
      {messages.map((msg, index) => (
        <Table striped bordered>
          <tbody>
            <tr key={index}>
              <td>
                {msg.msg} - {msg.username}
              </td>
            </tr>
          </tbody>
        </Table>
      ))}
    </div>
  );
};

export default MessageContainer;
