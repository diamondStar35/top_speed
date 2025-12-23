/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <Common/If/Network.h>
//#include <winsock2.h> 


// Link with Ws2_32.lib
UInt
Network::localIP( )
{
    

    WSADATA wsaData;
    struct hostent *pHostEnt; 
    struct sockaddr_in tmpSockAddr; //placeholder for the ip address

    // Not needed if it is already taken care by some other part of the application
    WSAStartup(MAKEWORD(2,0),&wsaData); 

    Char hostname[128];
    if (::gethostname(hostname, 128))
    {
        COMMON("(!) Network::localIP( ) : gethostname() failed with errno = %d", WSAGetLastError( ));
        return 0;
    }

    //This will retrieve the ip details and put it into pHostEnt structure
    pHostEnt = gethostbyname(hostname);

    if(pHostEnt == NULL)
    {
        COMMON("(!) Network::localIP( ) : Error occured: %s", GetLastError());
        return 0;
    }

    memcpy(&tmpSockAddr.sin_addr,pHostEnt->h_addr,pHostEnt->h_length);

    Char Ip[17];

    strcpy_s(Ip,inet_ntoa(tmpSockAddr.sin_addr));
    COMMON("Network::localIP() : Ip Address of the machine %s is %s", hostname, Ip);

    // Not needed if it is already taken care by some other part of the application
    WSACleanup();
    return inet_addr(Ip);
}





