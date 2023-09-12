import React, { createContext, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { SIGNALR_BASE_URL } from '../../config';

const SignalRContext = createContext();

export const SignalRProvider = ({ children }) => {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(SIGNALR_BASE_URL, {
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
