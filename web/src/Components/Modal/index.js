// UserNameModal.js
import React, { useState, useContext, useEffect } from 'react';
import SignalRContext from '../SignalR';
import { generateRandomNickname } from '../../Common/utils'; // Import the nickname generator function

const UserNameModal = ({ isOpen, onClose, onNameSubmit }) => {
    const connection = useContext(SignalRContext);
    const [name, setName] = useState(generateRandomNickname()); // Set the initial name to a random nickname
    const [connectionEstablished, setConnectionEstablished] = useState(false);

    useEffect(() => {
        if (!connectionEstablished) {
            console.log("Connetion is not established.Connecting to the server...");
            connection
                .start()
                .then(() => {
                    console.log("Connection established");
                    setConnectionEstablished(true);
                })
                .catch((error) => {
                    console.error(`SignalR Connection Error: ${error}`);
                });
        }
    }, []);

    const handleSubmit = (e) => {
        e.preventDefault();
        if (name.trim() !== '') {
            connection.invoke('JoinChat', name);
            onNameSubmit(name);
            onClose();
        }
    };

    const handleRefreshName = () => {
        // Generate a new random nickname and set it as the name state
        const newRandomName = generateRandomNickname();
        setName(newRandomName);
    };

    return (
        <div
            className={`fixed inset-0 flex items-center justify-center z-50 ${isOpen ? '' : 'hidden'
                }`}
        >
            <div className="bg-white p-4 rounded-lg shadow-md w-1/4">
                {connectionEstablished ? (
                    <>
                        <h2 className="text-xl font-semibold mb-2">Enter your name:</h2>
                        <form onSubmit={handleSubmit}>
                            <input
                                type="text"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                className="border p-2 rounded w-full"
                            />
                            <button
                                type="submit"
                                className="mt-2 bg-blue-500 text-white p-2 rounded hover:bg-blue-600 cursor-pointer"
                            >
                                Submit
                            </button>
                        </form>
                        <button
                            onClick={handleRefreshName}
                            className="mt-2 bg-gray-300 text-gray-700 p-2 rounded hover:bg-gray-400 cursor-pointer"
                        >
                            Refresh Name
                        </button>
                    </>
                ) : (
                    <p>Connecting to the server...</p>
                )}
            </div>
        </div>
    );
};

export default UserNameModal;
