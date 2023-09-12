import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import "tw-elements-react/dist/css/tw-elements-react.min.css";
import App from './App';
import { SignalRProvider } from './Components/SignalR';


const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <SignalRProvider>
      <App />
    </SignalRProvider>
  </React.StrictMode>
);
