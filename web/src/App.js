// App.js
import React from 'react';
import Canvas from './Components/Canvas';
import Topic from './Components/Topic';
import Chat from './Components/Chat';

const App = () => {
  const currentTopic = "Cat"; // You should manage this state

  return (
    <div class="grid grid-rows-6 grid-cols-2 h-screen">
      <div class="row-span-1 col-span-2 flex items-center justify-center bg-gray-200">
        <div class="text-center text-lg font-semibold p-1">
          Draw N guess
        </div>
      </div>

      <div class="row-span-5 col-span-1 bg-blue-200">
        <div class="p-4">
          Canvas
        </div>
        <Canvas />
      </div>

      <div class="row-span-1 col-span-1 bg-green-200">
        <div class="p-4">
          Topic
        </div>
        <Topic topic={currentTopic} />
      </div>

      <div class="row-span-4 col-span-1 bg-yellow-200">
        <div class="p-4">
          Chat
        </div>
        <Chat />
      </div>
    </div>

  );
};

export default App;
