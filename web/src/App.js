// App.js
import React, {useState, useEffect, useContext} from 'react';
import Canvas from './Components/Canvas';
import Topic from './Components/Topic';
import Chat from './Components/Chat';
import UserNameModal from './Components/Modal';

const App = () => {
  const currentTopic = "Cat"; 
  const [modalOpened, setModalOpened] = useState(true);
  const [userName, setUserName] = useState("");

  useEffect(() => {
    document.title = 'Draw N Guess';
  }, []);

  const handleNameSubmit = (name) => {
    console.log(name);
    setUserName(name);
  };

  const handleModalClose = () => {
    console.log("Modal closed");
    setModalOpened(false);
  };

  return (
    <div className="grid grid-rows-6 grid-cols-2 h-screen">
      <div className="row-span-1 col-span-2 flex items-center justify-center bg-gray-200">
        <div className="text-center text-lg font-semibold p-1">
          Draw N Guess
        </div>
      </div>

      <UserNameModal isOpen={modalOpened} onClose={handleModalClose} onNameSubmit={handleNameSubmit}/>

      <div className="row-span-5 col-span-1 bg-blue-200">
        <Canvas />
      </div>

      <div className="row-span-1 col-span-1 bg-green-200">
        <Topic topic={currentTopic} />
      </div>

      <div className="row-span-4 col-span-1 bg-yellow-200">
        <Chat userName={userName}/>
      </div>
    </div>

  );
};

export default App;
