import React, { useState, useEffect, useContext } from 'react';
import SignalRContext from '../SignalR';

const Game = ({ gameStatus }) => {
  const [timeLeft, setTimeLeft] = useState(60);
  const [isDrawer, setIsDrawer] = useState(false);
  const [playerCount, setPlayerCount] = useState(0);
  const connection = useContext(SignalRContext);

  useEffect(() => {
    let timer;

    connection.on("UpdateCounter", (count) =>{
      setPlayerCount(count);
    })

    setIsDrawer(gameStatus.Drawer === connection.connectionId);
    const calLeftTime = Math.max(Math.round((new Date(gameStatus.Time) - Date.now()) / 1000), 0)
    setTimeLeft(calLeftTime);
    
    timer = setInterval(() => {
      setTimeLeft((prevTime) => Math.max(prevTime - 1, 0));
    }, 1000);

    return () => {
      clearInterval(timer);
    };
  }, [isDrawer,gameStatus]);

  return (
    <div>
      <h2 className="text-left text-lg font-bold pl-2">Game Status</h2>
      <h2 className="text-left text-lg font-bold pl-2">Players: {playerCount}</h2>
      {isDrawer ? (
          <h2 className="text-left text-lg font-semibold pl-2"><div className='text-'>Word: {gameStatus.Word}</div></h2>
      ) : (
        <h2 className="text-left text-lg font-semibold pl-2">Word: Hidden</h2>
      )}
      <h2 className="text-left text-lg font-semibold pl-2">
            Time Left: {timeLeft} seconds
          </h2>
      <h2 className={`text-left text-xl font-semibold pl-2 ${isDrawer ? 'text-pink-600 animate-bounce' : ''}`}>
         You're {isDrawer ? 'drawing' : 'guessing'}
      </h2>
    </div>
  );
};

export default Game;
