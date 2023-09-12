import React, { useRef, useState, useEffect, useContext } from 'react';
import SignalRContext from '../SignalR';

const Canvas = () => {
  const [drawing, setDrawing] = useState(false);
  const [previousPosition, setPreviousPosition] = useState(null);
  const canvasRef = useRef(null);
  const storedImageData = useRef(null);
  const connection = useContext(SignalRContext);

  useEffect(() => {
    const canvas = canvasRef.current;
    const parent = canvas.parentElement;
    const ctx = canvas.getContext('2d');

    const drawReceivedData = (drawData) => {
      const canvas = canvasRef.current;
      const ctx = canvas.getContext('2d');
      ctx.beginPath();
      ctx.moveTo(drawData.x1, drawData.y1);
      ctx.lineTo(drawData.x2, drawData.y2);
      ctx.stroke();
    };

    connection.on('ReceiveDraw', (drawData) => {
      // Handle received drawing data and draw it on the canvas of all users
      drawReceivedData(drawData);
    });

    // Function to update canvas dimensions
    const updateCanvasDimensions = () => {
      // Store the current canvas content
      storedImageData.current = ctx.getImageData(0, 0, canvas.width, canvas.height);

      canvas.width = parent.clientWidth;
      canvas.height = parent.clientHeight;

      // Redraw stored content on the resized canvas
      if (storedImageData.current) {
        ctx.putImageData(storedImageData.current, 0, 0);
      }

      // Redraw content as needed (e.g., clear and redraw)
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = 'white';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
    };

    // Initial canvas setup
    updateCanvasDimensions();

    // Event listener for window resize
    window.addEventListener('resize', updateCanvasDimensions);

    // Cleanup event listener on component unmount
    return () => {
      window.removeEventListener('resize', updateCanvasDimensions);
    };
  }, []);

  const startDrawing = (e) => {
    setDrawing(true);
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    ctx.beginPath();
    const x = e.nativeEvent.offsetX;
    const y = e.nativeEvent.offsetY;
    ctx.moveTo(x, y);
    setPreviousPosition({ x, y });
  };

  const draw = (e) => {
    if (!drawing) return;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    const x = e.nativeEvent.offsetX;
    const y = e.nativeEvent.offsetY;
    ctx.lineTo(x, y);
    ctx.stroke();
    const drawData = { x1: previousPosition.x, y1: previousPosition.y, x2: x, y2: y };
    connection.invoke('SendDraw', JSON.stringify(drawData));
    setPreviousPosition({ x, y });
  };

  const stopDrawing = () => {
    setDrawing(false);
    setPreviousPosition(null);
    const canvas = canvasRef.current;
    canvas.getContext('2d').closePath();
  };

  return (
    <div style={{width: '100%', height:'100%'}}>
      {/* canvas to fulfill the entire container */}
      
      <canvas
        ref={canvasRef}
        onMouseDown={startDrawing}
        onMouseMove={draw}
        onMouseUp={stopDrawing}
        onMouseOut={stopDrawing}
        className="cursor-pointer"
        style={{ width: '100%', height: '100%' }}
      ></canvas>
    </div>
  );
};


export default Canvas;