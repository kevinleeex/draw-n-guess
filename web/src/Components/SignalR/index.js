import React, { createContext, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const baseUrl = 'http://localhost:7071/api/'; // Azure Function endpoint
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(baseUrl, {
            headers: {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Credentials': 'true',
            },                                                                   
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    return (
        <SignalRContext.Provider value={connection}>{children}</SignalRContext.Provider>
    );
};

export default SignalRContext;
