import React, { useState, useEffect } from 'react';

const Game = ({ topic, user, isDrawer }) => {
  const [timeLeft, setTimeLeft] = useState(60);

  useEffect(() => {
    let timer;
    
    if (isDrawer) {
      timer = setInterval(() => {
        setTimeLeft((prevTime) => prevTime - 1);
      }, 1000);
    }

    return () => {
      clearInterval(timer);
    };
  }, [isDrawer]);

  return (
    <div>
      <h2 className="text-left text-lg font-bold pl-2">Game Status</h2>
      {isDrawer ? (
        <>
          <h2 className="text-left text-lg font-semibold pl-2">Word: {topic}</h2>
          <h2 className="text-left text-lg font-semibold pl-2">
            Time Left: {timeLeft} seconds
          </h2>
        </>
      ) : (
        <h2 className="text-left text-lg font-semibold pl-2">Word: Hidden</h2>
      )}
      <h2 className="text-left text-lg font-semibold pl-2">
         You're {isDrawer ? 'drawing' : 'guessing'}
      </h2>
    </div>
  );
};

export default Game;
