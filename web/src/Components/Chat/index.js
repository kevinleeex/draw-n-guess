import React, { useState, useEffect, useRef, useContext } from 'react';
import SignalRContext from '../SignalR';

const Chat = ({}) => {
  const [messages, setMessages] = useState([]);
  const [inputText, setInputText] = useState('');
  const inputRef = useRef(null);
  const connection = useContext(SignalRContext);

  // Simulated data for the chat messages
  const initialMessages = [
    { Id: 1, Name: 'You', Text: "It's an animal", Own: true, SystemMessage: false },
    { Id: 2, Name: 'Friend', Text: 'Dog', Own: false, SystemMessage: false },
  ];

  useEffect(() => {
    const handleReceivedMessage = (message) => {
      console.log('receivedmessage called');
      console.log(JSON.stringify(message));
      setMessages((prevMessages) => [...prevMessages, message]);
    };
  
    connection.on('receivedmessage', handleReceivedMessage);
  
    // Initialize messages
    console.log('Initialize messages');
    setMessages(initialMessages);
  
    // Remove the event handler when the component is unmounted
    return () => {
      connection.off('receivedmessage');
    };
  }, []);

  const handleInputChange = (e) => {
    setInputText(e.target.value);
  };

  const handleSendMessage = async () => {
    if (inputText.trim() === '') return;

    await connection.invoke('SendMessage', inputText);
    
    setInputText('');
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      handleSendMessage();
    }
  };

  useEffect(() => {
    // Auto-scroll to the bottom of the chat when new messages arrive
    inputRef.current.scrollTop = inputRef.current.scrollHeight;
  }, [messages]);

  return (
    <div className="w-full max-w-screen-lg mx-auto p-4 flex flex-col h-full bg-gray-100">
      <div className="flex-grow flex flex-col space-y-2 overflow-y-auto" ref={inputRef}>
        {messages.map((message) => (
          <div
            key={message.Id}
            className={`${message.SystemMessage ? 'self-start bg-green-300 text-black': message.Own || message.ConnectionId === connection.connectionId ? 'self-end bg-blue-500 text-white' : 'self-start bg-white text-black'
            } p-2 rounded-lg max-w-md`}
          >
            <p className="font-semibold">{message.Name}</p>
            <p>{message.Text}</p>
          </div>
        ))}
      </div>
      <div className="mt-4">
        <div className="flex">
          <input
            type="text"
            className="flex-grow p-2 border rounded-l-lg"
            placeholder="Type your message..."
            value={inputText}
            onChange={handleInputChange}
            onKeyPress={handleKeyPress}
          />
          <button
            className="bg-blue-500 text-white p-2 rounded-r-lg"
            onClick={handleSendMessage}
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
};

export default Chat;
