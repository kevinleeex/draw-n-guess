import React, { useState, useEffect, useContext } from 'react';
import SignalRContext from '../SignalR';

const Game = ({ gameStatus }) => {
  const [timeLeft, setTimeLeft] = useState(60);
  const [isDrawer, setIsDrawer] = useState(false);
  const connection = useContext(SignalRContext);

  useEffect(() => {
    let timer;

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
      {isDrawer ? (
        <>
          <h2 className="text-left text-lg font-semibold pl-2">Word: {gameStatus.Word}</h2>
        </>
      ) : (
        <h2 className="text-left text-lg font-semibold pl-2">Word: Hidden</h2>
      )}
      <h2 className="text-left text-lg font-semibold pl-2">
            Time Left: {timeLeft} seconds
          </h2>
      <h2 className="text-left text-lg font-semibold pl-2">
         You're {isDrawer ? 'drawing' : 'guessing'}
      </h2>
    </div>
  );
};

export default Game;
