/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "DxCommon/If/Network.h"
#include <tchar.h>


namespace DirectX
{

Server::Server( ) :
    m_directPlayServer(0),
    m_directPlayAddress(0),
    m_iServer(0),
    m_started(false)
{
    DXCOMMON("(+) Server");
    ZeroMemory(&m_directPlayAppDesc, sizeof(DPN_APPLICATION_DESC));
}


Server::~Server( )
{
    DXCOMMON("(-) Server");
}


Int
Server::startSession(Char* name, UInt port)
{
    if (m_started)
        stopSession( );

    DXCOMMON("Server::startSession(%s, %d)", name, port);


    HRESULT hr;
    if (FAILED(hr = CoCreateInstance(CLSID_DirectPlay8Server,
                                     NULL,
                                     CLSCTX_INPROC_SERVER,
                                     IID_IDirectPlay8Server,
                                     (void**) &m_directPlayServer)))
    {
        DXCOMMON("(!) Server::startSession : error creating DirectPlay8Server, errno = %x", hr);
        return dxFailed;
    }

    if (FAILED(hr = m_directPlayServer->Initialize(this, directPlayMessageHandler, 0)))
    {
        DXCOMMON("(!) Server::startSession : error initializing DirectPlay8Server, errno = %x", hr);
        return dxFailed;
    }


    if (FAILED(hr = CoCreateInstance(CLSID_DirectPlay8Address, 
                                     NULL,
                                     CLSCTX_ALL, 
                                     IID_IDirectPlay8Address,
                                     (void**) &m_directPlayAddress)))
    {
        DXCOMMON("(!) Server::startSession : error creating DirectPlayAddress, errno = %x", hr);
        SAFE_DELETE(m_directPlayAddress);
        return dxFailed;        
    }

    if (FAILED(hr = m_directPlayAddress->SetSP(&CLSID_DP8SP_TCPIP)))
    {
        DXCOMMON("(!) Server::startSession : error setting service provider, errno = %x", hr);
        SAFE_DELETE(m_directPlayAddress);
        return dxFailed;
    }

    // Add the port to pDP8AddrLocal, if the port is non-zero.
    // If the port is 0, then DirectPlay will pick a port, 
    // Games will typically hard code the port so the 
    // user need not know it
    if (port != 0)
    {
        if (FAILED(hr = m_directPlayAddress->AddComponent(DPNA_KEY_PORT, 
                                                          &port, 
                                                          sizeof(port),
                                                          DPNA_DATATYPE_DWORD)))
        {
            DXCOMMON("(!) Server::startSession : error setting port, errno = %x", hr);
            SAFE_DELETE(m_directPlayAddress);
            return dxFailed;
        }
    }


    WCHAR wstrName[MAX_PATH];
    convertStringToWide(wstrName, name, MAX_PATH);

    ZeroMemory(&m_directPlayAppDesc, sizeof(DPN_APPLICATION_DESC));
    m_directPlayAppDesc.dwSize           = sizeof(DPN_APPLICATION_DESC);
    m_directPlayAppDesc.dwFlags          = DPNSESSION_CLIENT_SERVER;
    m_directPlayAppDesc.guidApplication  = m_applicationGUID;
    m_directPlayAppDesc.pwszSessionName  = wstrName;

    if (FAILED(hr = m_directPlayServer->Host(&m_directPlayAppDesc, 
                                             &m_directPlayAddress,
                                             1, NULL, NULL, NULL, 0)))
    {
        DXCOMMON("(!) Server::startSession : Host call failed, errno = %x", hr);
        SAFE_DELETE(m_directPlayAddress);
        return dxFailed;        
    }
    m_started = true;
    return dxSuccess;
}



void
Server::stopSession( )
{
    if (m_directPlayServer != 0)
        m_directPlayServer->Close(0);

    SAFE_RELEASE(m_directPlayAddress);
    
    SAFE_RELEASE(m_directPlayServer);

    m_started = false;
}



void
Server::sendPacket(UInt to, void* buffer, UInt size, Boolean secure, UInt timeout)
{
    DPNHANDLE       hAsync;
    DPNHANDLE*      phAsync;
    DWORD           dwFlags = 0;
    DPN_BUFFER_DESC dpnBufferDesc;

    if (secure)
    {
        // If we are guaranteed then we must specify
        // DPNSEND_NOCOMPLETE and pass in non-null for the 
        // pvAsyncContext
        dwFlags = DPNSEND_GUARANTEED;
    }
    else
    {
        // If we aren't guaranteed then we can
        // specify DPNSEND_NOCOMPLETE.  And when 
        // DPNSEND_NOCOMPLETE is on pvAsyncContext
        // must be NULL.
        dwFlags = DPNSEND_NOCOMPLETE;
    }
    // Must define an async handle for the SendTo call. 
    phAsync = &hAsync;

    dpnBufferDesc.dwBufferSize = size;
    dpnBufferDesc.pBufferData = (PBYTE) buffer;

    // DirectPlay will tell via the message handler 
    // if there are any severe errors, so ignore any errors 
    m_directPlayServer->SendTo(to, &dpnBufferDesc, 1, timeout, NULL, phAsync, dwFlags);
}








HRESULT WINAPI  
Server::directPlayMessageHandler(void* pvUserContext, DWORD dwMessageId, void* pMsgBuffer)
{
    Server* object = (Server*) pvUserContext;

    return object->localMessageHandler(/* pvUserContext, */ dwMessageId, pMsgBuffer);
}



HRESULT WINAPI 
Server::localMessageHandler(/* void* context, */
                            UInt messageId,
                            void* msgBuffer)
{
    // Try not to stay in this message handler for too long, otherwise
    // there will be a backlog of data.  The best solution is to 
    // queue data as it comes in, and then handle it on other threads.
    
    // This function is called by the DirectPlay message handler pool of 
    // threads, so be careful of thread synchronization problems with shared memory

    switch (messageId)
    {
        case DPN_MSGID_CREATE_PLAYER:
        {
            PDPNMSG_CREATE_PLAYER pCreatePlayer = (PDPNMSG_CREATE_PLAYER) msgBuffer;
            DWORD dwSize = 0;
            DPN_PLAYER_INFO* pdpPlayerInfo = NULL;
            HRESULT hr = m_directPlayServer->GetClientInfo(pCreatePlayer->dpnidPlayer, 
                                            pdpPlayerInfo, &dwSize, 0 );
            if( FAILED(hr) && hr != DPNERR_BUFFERTOOSMALL )
            {
                if( hr == DPNERR_INVALIDPLAYER )
                {
                    // Ignore this message if this is for the host
                    DXCOMMON("Server : received create_player from host itself, ignoring...");
                    break;
                }
            }
            DXCOMMON("Server : received create_player");

            if ((pCreatePlayer->pvPlayerContext == NULL) && (m_iServer))
                m_iServer->onAddConnection(pCreatePlayer->dpnidPlayer);
            break;
        }
        
        case DPN_MSGID_DESTROY_PLAYER:
        {
            PDPNMSG_DESTROY_PLAYER pDestroyPlayer = (PDPNMSG_DESTROY_PLAYER) msgBuffer;

            if ((pDestroyPlayer->pvPlayerContext == NULL) && (m_iServer))
                m_iServer->onRemoveConnection(pDestroyPlayer->dpnidPlayer);
            break;
        }

        case DPN_MSGID_RECEIVE:
        {
            PDPNMSG_RECEIVE pRecvData = (PDPNMSG_RECEIVE) msgBuffer;

            if (m_iServer)
                m_iServer->onPacket(pRecvData->dpnidSender, pRecvData->pReceiveData, pRecvData->dwReceiveDataSize);
            break;
        }

        case DPN_MSGID_TERMINATE_SESSION:
        {
            if (m_iServer)
                m_iServer->onSessionLost( );
            break;
        }
    }

    return DPN_OK;
}



Client::Client( ) : 
    m_directPlayClient(NULL),
    m_iClient(NULL),
    m_directPlayEnumHandle(NULL),
    m_nSessions(0)
{
    DXCOMMON("(+) Client");
}    


Client::~Client( )
{
    DXCOMMON("(-) Client");
}


Int
Client::initialize( )
{
    DXCOMMON("Client::initialize");
    HRESULT hr;
    if (FAILED(hr = CoCreateInstance(CLSID_DirectPlay8Client, NULL, 
                                     CLSCTX_ALL, IID_IDirectPlay8Client,
                                     (void**) &m_directPlayClient)))
    {
        DXCOMMON("(!) Client : failed to create DirectPlay Client, errno = 0x%x", hr);
        return dxFailed;
    }

    if (FAILED(hr = m_directPlayClient->Initialize(this, directPlayMessageHandler, 0)))
    {
        DXCOMMON("(!) Client::initialize : failed to initialize DirectPlay Client, errno = 0x%x", hr);
        return dxFailed;
    }
    // initialize m_sessions
    for (int i = 0; i < DXCOMMON_NMAXSESSIONS; ++i)
    {
        m_sessions[i].hostAddress = 0;
        m_sessions[i].deviceAddress = 0;
    }
    
    return dxSuccess;
}


Int
Client::finalize( )
{
    DXCOMMON("Client::finalize");
    /*
    for( DWORD dwIndex = 0; dwIndex < MAX_SESSIONS; dwIndex++ )
    {
        SAFE_RELEASE( m_pHostAddresses[dwIndex] );
        SAFE_RELEASE( m_pDeviceAddresses[dwIndex] );
    }

    if( m_dpnhEnum != NULL )
    {
        // Cancel the enumeration if its in progress, and ignore any errors
        m_pDPlay->CancelAsyncOperation( m_dpnhEnum, 0 );
        m_dpnhEnum = NULL;
    }
    */

    if (m_directPlayClient != NULL)
        m_directPlayClient->Close(0);

    SAFE_RELEASE(m_directPlayClient);
    return dxSuccess;
}



Int
Client::sendPacket(void* buffer, UInt size, 
                   Boolean secure, UInt timeout)
{
    if (m_directPlayClient == NULL)
    {
        DXCOMMON("(!) Client::sendPacket : DirectPlay client not initialized");
        return dxFailed;
    }

    DPNHANDLE  hAsync;
    DWORD      flags = 0;
    DPNHANDLE* phAsync;

    if (secure)
    {
        // If we are guaranteed then we must specify
        // DPNSEND_NOCOMPLETE and pass in non-null for the 
        // pvAsyncContext
        flags = DPNSEND_GUARANTEED;
    }
    else
    {
        // If we aren't guaranteed then we can
        // specify DPNSEND_NOCOMPLETE.  And when 
        // DPNSEND_NOCOMPLETE is on pvAsyncContext
        // must be NULL.
        flags = DPNSEND_NOCOMPLETE;
    }
    
    // Must pass in a value for the Asyn handle. Will be thrown when proc completes.
    phAsync = &hAsync;

    DPN_BUFFER_DESC dpnBufferDesc;
    dpnBufferDesc.dwBufferSize = size;
    dpnBufferDesc.pBufferData = (PBYTE) buffer;

    // DirectPlay will tell via the message handler 
    // if there are any severe errors, so ignore any errors 
    m_directPlayClient->Send(&dpnBufferDesc, 1, timeout, NULL, phAsync, flags);
    return dxSuccess;
}



Int 
Client::startSessionEnum(UInt port, const Char* ipaddress) 
{
    DXCOMMON("Client::startSessionEnum");
    if (m_directPlayClient == NULL)
    {
        DXCOMMON("(!) Client::startSessionEnum : DirectPlay client not initialized");
        return dxFailed;
    }

    DPN_APPLICATION_DESC   appDesc;
    IDirectPlay8Address*   addressHost  = NULL;
    IDirectPlay8Address*   addressLocal = NULL;
    WCHAR*                 hostname     = NULL;
    HRESULT                hr;

    if (m_directPlayEnumHandle != NULL )
    {
        // If an enumeration is already running, cancel 
        // it and start a new one.  Ignore any errors from CancelAsyncOperation
        m_directPlayClient->CancelAsyncOperation(m_directPlayEnumHandle, 0);
        m_directPlayEnumHandle = NULL;
    }

    m_nSessions = 0;

    // Create the local device address object
    if (FAILED(hr = CoCreateInstance(CLSID_DirectPlay8Address, NULL, 
                                     CLSCTX_ALL, IID_IDirectPlay8Address,
                                     (void**) &addressLocal)))
    {
        DXCOMMON("(!) Client::startSessionEnum : failed to create DirectPlay localaddress, errno = 0x%x", hr);
        return dxFailed;
    }

    // Set IP service provider
    if (FAILED(hr = addressLocal->SetSP(&CLSID_DP8SP_TCPIP)))
    {
        DXCOMMON("(!) Client::startSessionEnum : failed to set service provider, errno = 0x%x", hr);
        SAFE_RELEASE(addressLocal);
        return dxFailed;
    }

    // Create the remote host address object
    if (FAILED(hr = CoCreateInstance(CLSID_DirectPlay8Address, NULL, 
                                     CLSCTX_ALL, IID_IDirectPlay8Address,
                                     (void**) &addressHost)))
    {
        DXCOMMON("(!) Client::startSessionEnum : failed to create DirectPlay hostaddress, errno = 0x%x", hr);
        SAFE_RELEASE(addressLocal);
        return dxFailed;
    }

    // Set IP service provider
    if (FAILED(hr = addressHost->SetSP(&CLSID_DP8SP_TCPIP)))
    {
        DXCOMMON("(!) Client::startSessionEnum : failed to set host service provider, errno = 0x%x", hr);
        SAFE_RELEASE(addressLocal);
        SAFE_RELEASE(addressHost);
        return dxFailed;
    }

    if (FAILED(hr = addressHost->AddComponent(DPNA_KEY_PORT, &port, sizeof(port), DPNA_DATATYPE_DWORD)))
    {
        DXCOMMON("(!) Client::startSessionEnum : failed to add port compononent, port = %d, errno = 0x%x", hr);
        SAFE_RELEASE(addressLocal);
        SAFE_RELEASE(addressHost);
        return dxFailed;
    }

    // Set the remote host name (if provided)
    if (ipaddress != NULL && ipaddress[0] != 0)
    {
        hostname = new WCHAR[_tcslen(ipaddress)+1];

        convertStringToWide(hostname, ipaddress, _tcslen(ipaddress)+1);

        if (FAILED(hr = addressHost->AddComponent(DPNA_KEY_HOSTNAME, hostname, 
                                                  (wcslen(hostname)+1)*sizeof(WCHAR), 
                                                  DPNA_DATATYPE_STRING)))
        {
            DXCOMMON("(!) Client::startSessionEnum : failed to add hostname component, errno = 0x%x", hr);
            SAFE_RELEASE(addressLocal);
            SAFE_RELEASE(addressHost);
            SAFE_DELETE(hostname);
            return dxFailed;
        }
    }

    ZeroMemory(&appDesc, sizeof(DPN_APPLICATION_DESC));
    appDesc.dwSize = sizeof(DPN_APPLICATION_DESC);
    appDesc.guidApplication = m_applicationGUID;

    // Enumerate all StressMazeApp hosts running on IP service providers
    hr = m_directPlayClient->EnumHosts(&appDesc, addressHost, 
                                       addressLocal, NULL, 
                                       0, INFINITE, 0, INFINITE, NULL, 
                                       &m_directPlayEnumHandle, 0);
    if (hr != DPNERR_PENDING && FAILED(hr))
    {
        DXCOMMON("(!) Client::startSessionEnum : failed to enum hosts, errno = 0x%x", hr);
        SAFE_RELEASE(addressLocal);
        SAFE_RELEASE(addressHost);
        SAFE_DELETE(hostname);
        return dxFailed;
    }
    return dxSuccess;
}




Int Client::stopSessionEnum( )
{
    DXCOMMON("Client::stopSessionEnum");
    if (m_directPlayClient == NULL)
    {
        DXCOMMON("(!) Client::stopSessionEnum : not initialized");
        return dxFailed;
    }

    // If an enumeration is already running, cancel it and ignore
    // any errors from CancelAsyncOperation
    if (m_directPlayEnumHandle != NULL)
    {
        DXCOMMON("Client::stopSessionEnum : cancelling async operations");
        m_directPlayClient->CancelAsyncOperation(m_directPlayEnumHandle, 0);
        m_directPlayEnumHandle = NULL;
    }

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: 
// Desc: 
//-----------------------------------------------------------------------------
void
Client::sessionEnumCallback(const DPN_APPLICATION_DESC* appDesc, 
                            IDirectPlay8Address* addressHost,
                            IDirectPlay8Address* addressDevice)
{
    DXCOMMON("Client::sessionEnumCallback");
    Mutex::Guard guard(m_mutex);

    if (m_directPlayClient == NULL)
    {
        DXCOMMON("(!) Client::sessionEnumCallback : directplay not initialized");
        return;
    }

    if (m_nSessions < DXCOMMON_NMAXSESSIONS)
    {
        // Search for existing record for this session, if 
        // there is one, break this loop so we just update
        // the current entry.
        UInt i;
        for (i = 0; i < m_nSessions; i++)
        {
            if (m_sessions[i].appDesc.guidInstance == appDesc->guidInstance)
                break;
        }
        
        memcpy(&m_sessions[i].appDesc, appDesc, sizeof(DPN_APPLICATION_DESC));

        // Copy pDP8AddressHost to m_pHostAddresses[dwIndex]
        SAFE_RELEASE(m_sessions[i].hostAddress);

        addressHost->QueryInterface(IID_IDirectPlay8Address, 
                                    (void**) &m_sessions[i].hostAddress);

        // Copy pDP8AddressDevice to m_pDeviceAddresses[dwIndex]
        SAFE_RELEASE(m_sessions[i].deviceAddress);
        
        addressDevice->QueryInterface(IID_IDirectPlay8Address, 
                                      (void**) &m_sessions[i].deviceAddress);

        if (appDesc->pwszSessionName != NULL)
        {
            convertWideToString(m_sessions[i].sessionName, appDesc->pwszSessionName);
        }
        else
        {
            _tcscpy(m_sessions[i].sessionName, TEXT("Untitled"));
        }

        if (i == m_nSessions)
            m_nSessions++;
    }

    return ;
}


Int
Client::session(UInt i, SessionInfo& info)
{
    if (i >= m_nSessions)
        return dxFailed;
    info.appDesc = m_sessions[i].appDesc;
    strcpy(info.sessionName, m_sessions[i].sessionName);
    return dxSuccess;     
}


Int
Client::joinSession(UInt i)
{
    DXCOMMON("Client::joinSession %d", i);
    
    HRESULT hr;
    // IDirectPlay8Address* hostAddress = NULL;
    // IDirectPlay8Address* deviceAddress = NULL;

    if (m_directPlayClient == NULL)
    {
        DXCOMMON("(!) Client::joinSession : directplay not initialized");
        return dxFailed;
    }

    Mutex::Guard guard(m_mutex);

    
    m_sessions[i].deviceAddress->AddRef( );
    m_sessions[i].hostAddress->AddRef( );

    // Connect to the remote host
    // The enumeration is automatically canceled after Connect is called 
    if (FAILED(hr = m_directPlayClient->Connect(
        &m_sessions[i].appDesc,        // Application description
        m_sessions[i].hostAddress,     // Session host address
        m_sessions[i].deviceAddress,   // Address of device used to connect to the host
        NULL, NULL,         // Security descriptions & credientials (MBZ in DPlay8)
        NULL, 0,            // User data & its size
        NULL,               // Asynchronous connection context (returned with DPNMSG_CONNECT_COMPLETE in async handshaking)
        NULL,               // Asynchronous connection handle (used to cancel connection process)
        DPNOP_SYNC)))       // Connect synchronously
    {
        if (hr == DPNERR_NORESPONSE || hr == DPNERR_ABORTED)
        {
            DXCOMMON("(!) Client::joinSession : server exited while joining");
        }
        else if (hr == DPNERR_INVALIDINSTANCE)
        {
            DXCOMMON("(!) Client::joinSession : original server exited and another server came online");
        }
        
        DXCOMMON("(!) Client::joinSession : failed, errno = 0x%x", hr);
        SAFE_RELEASE(m_sessions[i].deviceAddress);
        SAFE_RELEASE(m_sessions[i].hostAddress);
        return dxFailed;
    }

    DXCOMMON("Client::joinSession : connected to server");

    // cleanup extra references
    SAFE_RELEASE(m_sessions[i].deviceAddress);
    SAFE_RELEASE(m_sessions[i].hostAddress);
    
    return dxSuccess;
}


Int
Client::joinSessionAt(UInt port, const Char* ipaddress)
{
    DXCOMMON("Client::joinSessionAt : port = %d, address = %s", port, ipaddress);

    Mutex::Guard guard(m_mutex);
    
    if (m_directPlayClient == NULL)
    {
        DXCOMMON("(!) Client::joinSessionAt : directplay not initialized");
        return dxFailed;
    }
    HRESULT hr;
    WCHAR*                 hostname     = NULL;

    // Construct the application description
    DPN_APPLICATION_DESC   appDesc;
    ZeroMemory(&appDesc, sizeof(DPN_APPLICATION_DESC));
    appDesc.dwSize = sizeof(DPN_APPLICATION_DESC);
    appDesc.guidApplication = m_applicationGUID;

    // Create the remote host address object
    IDirectPlay8Address*   addressHost  = NULL;
    if (FAILED(hr = CoCreateInstance(CLSID_DirectPlay8Address, NULL, 
                                     CLSCTX_ALL, IID_IDirectPlay8Address,
                                     (void**) &addressHost)))
    {
        DXCOMMON("(!) Client::joinSessionAt : failed to create DirectPlay hostaddress, errno = 0x%x", hr);
        return dxFailed;
    }

    // Set IP service provider
    if (FAILED(hr = addressHost->SetSP(&CLSID_DP8SP_TCPIP)))
    {
        DXCOMMON("(!) Client::joinSessionAt : failed to set host service provider, errno = 0x%x", hr);
        SAFE_RELEASE(addressHost);
        return dxFailed;
    }

    if (FAILED(hr = addressHost->AddComponent(DPNA_KEY_PORT, &port, sizeof(port), DPNA_DATATYPE_DWORD)))
    {
        DXCOMMON("(!) Client::joinSessionAt : failed to add port compononent, port = %d, errno = 0x%x", hr);
        SAFE_RELEASE(addressHost);
        return dxFailed;
    }

    // Set the remote host name (if provided)
    if (ipaddress != NULL && ipaddress[0] != 0)
    {
        hostname = new WCHAR[_tcslen(ipaddress)+1];

        convertStringToWide(hostname, ipaddress, _tcslen(ipaddress)+1);

        if (FAILED(hr = addressHost->AddComponent(DPNA_KEY_HOSTNAME, hostname, 
                                                  (wcslen(hostname)+1)*sizeof(WCHAR), 
                                                  DPNA_DATATYPE_STRING)))
        {
            DXCOMMON("(!) Client::joinSessionAt : failed to add hostname component, errno = 0x%x", hr);
            SAFE_RELEASE(addressHost);
            SAFE_DELETE(hostname);
            return dxFailed;
        }
    }

    // Connect to the remote host
    // The enumeration is automatically canceled after Connect is called 
    if (FAILED(hr = m_directPlayClient->Connect(
        &appDesc,        // Application description
        addressHost,     // Session host address
        NULL,            // Address of device used to connect to the host
        NULL, NULL,      // Security descriptions & credientials (MBZ in DPlay8)
        NULL, 0,         // User data & its size
        NULL,            // Asynchronous connection context (returned with DPNMSG_CONNECT_COMPLETE in async handshaking)
        NULL,            // Asynchronous connection handle (used to cancel connection process)
        DPNOP_SYNC)))    // Connect synchronously
    {
        if (hr == DPNERR_NORESPONSE || hr == DPNERR_ABORTED)
        {
            DXCOMMON("(!) Client::joinSessionAt : server exited while joining");
        }
        else if (hr == DPNERR_INVALIDINSTANCE)
        {
            DXCOMMON("(!) Client::joinSession : original server exited and another server came online");
        }
        
        DXCOMMON("(!) Client::joinSessionAt : failed, errno = 0x%x", hr);
        SAFE_RELEASE(addressHost);
        SAFE_DELETE(hostname);
        return dxFailed;
    }

    DXCOMMON("Client::joinSession : connected to server");

    // cleanup extra references
    SAFE_RELEASE(addressHost);
    SAFE_DELETE(hostname);
    
    return dxSuccess;
}


HRESULT WINAPI  
Client::directPlayMessageHandler(void* pvUserContext, DWORD dwMessageId, void* pMsgBuffer)
{
    Client* object = (Client*) pvUserContext;

    return object->localMessageHandler(/* pvUserContext, */ dwMessageId, pMsgBuffer);
}



HRESULT WINAPI 
Client::localMessageHandler(/* void* context, */
                            UInt messageId,
                            void* msgBuffer)
{
    switch(messageId)
    {
        case DPN_MSGID_RECEIVE:
        {
            PDPNMSG_RECEIVE receiveMsg = (PDPNMSG_RECEIVE) msgBuffer;

            if (m_iClient != NULL )
            {
                m_iClient->onPacket(receiveMsg->dpnidSender, 
                                    receiveMsg->pReceiveData, 
                                    receiveMsg->dwReceiveDataSize);
            }
            break;
        }
        
        case DPN_MSGID_TERMINATE_SESSION:
        {
            PDPNMSG_TERMINATE_SESSION terminateMsg = (PDPNMSG_TERMINATE_SESSION) msgBuffer;

            // The MazeServer passes a DWORD in pvTerminateData if 
            // it disconnected us, otherwise it will be null.
            if (terminateMsg->pvTerminateData != NULL)
            {
                DWORD* pdw = (DWORD*) terminateMsg->pvTerminateData;
                DXCOMMON("Client::localMessageHandler : terminateSession received, reason = 0x%x", *pdw);
            }

            if (m_iClient != NULL)
                m_iClient->onSessionLost( );

            // Now that the session is lost we need to restart DirectPlay by calling
            // Close() and Init() on m_pDPlay, however this can not be 
            // done in the DirectPlay message callback, so the main thread will
            // do this when IsSessionLost() returns TRUE
            break;
        }

        case DPN_MSGID_ENUM_HOSTS_RESPONSE:
        {
            PDPNMSG_ENUM_HOSTS_RESPONSE enumHostsResponse = (PDPNMSG_ENUM_HOSTS_RESPONSE) msgBuffer;
            sessionEnumCallback(enumHostsResponse->pApplicationDescription, 
                                enumHostsResponse->pAddressSender,
                                enumHostsResponse->pAddressDevice);
            break;
        }
    }

    return DPN_OK;
}





}; // namespace DirectX
