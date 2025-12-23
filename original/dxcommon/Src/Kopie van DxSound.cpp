/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/DxCommon.h>
#include <dxerr8.h>


namespace DirectX
{


//-----------------------------------------------------------------------------
// Name: CSoundManager::CSoundManager()
// Desc: Constructs the class
//-----------------------------------------------------------------------------
SoundManager::SoundManager()
{
    m_pDS = NULL;
}




//-----------------------------------------------------------------------------
// Name: CSoundManager::~CSoundManager()
// Desc: Destroys the class
//-----------------------------------------------------------------------------
SoundManager::~SoundManager()
{
    SAFE_RELEASE( m_pDS ); 
}




//-----------------------------------------------------------------------------
// Name: CSoundManager::Initialize()
// Desc: Initializes the IDirectSound object and also sets the primary buffer
//       format.  This function must be called before any others.
//-----------------------------------------------------------------------------
Int SoundManager::Initialize( HWND  hWnd, 
                              DWORD dwCoopLevel, 
                              DWORD dwPrimaryChannels, 
                              DWORD dwPrimaryFreq, 
                              DWORD dwPrimaryBitRate )
{
    SAFE_RELEASE( m_pDS );
    // Create IDirectSound using the primary sound device
    if (FAILED(DirectSoundCreate8(NULL, &m_pDS, NULL)))
    {
        DXCOMMON("(!) SoundManager : failed to create IDirectSound interface.");        
        return dxFailed;
    }

    // Set DirectSound coop level 
    if (FAILED(m_pDS->SetCooperativeLevel( hWnd, dwCoopLevel)))
    {
        DXCOMMON("(!) SoundManager : failed to set Cooperative level.");
        return dxFailed;
    }
         
    // Set primary buffer format
    if (FAILED(SetPrimaryBufferFormat(dwPrimaryChannels, dwPrimaryFreq, dwPrimaryBitRate)))
    {
        DXCOMMON("(!) SoundManager : failed to set primary buffer format.");
        return dxFailed;
    }

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CSoundManager::SetPrimaryBufferFormat()
// Desc: Set primary buffer to a specified format 
//       For example, to set the primary buffer format to 22kHz stereo, 16-bit
//       then:   dwPrimaryChannels = 2
//               dwPrimaryFreq     = 22050, 
//               dwPrimaryBitRate  = 16
//-----------------------------------------------------------------------------
Int SoundManager::SetPrimaryBufferFormat( UInt dwPrimaryChannels, 
                                          UInt dwPrimaryFreq, 
                                          UInt dwPrimaryBitRate )
{
    LPDIRECTSOUNDBUFFER pDSBPrimary = NULL;

    if (m_pDS == 0)
    {
        DXCOMMON("(!) SoundManager::SetPrimaryBufferFormat : interface uninitialized.");
        return dxFailed;
    }

    // Get the primary buffer 
    DSBUFFERDESC dsbd;
    ZeroMemory( &dsbd, sizeof(DSBUFFERDESC) );
    dsbd.dwSize        = sizeof(DSBUFFERDESC);
    dsbd.dwFlags       = DSBCAPS_PRIMARYBUFFER;
    dsbd.dwBufferBytes = 0;
    dsbd.lpwfxFormat   = NULL;
       
    if (FAILED(m_pDS->CreateSoundBuffer(&dsbd, &pDSBPrimary, NULL)))
    {
        DXCOMMON("(!) SoundManager::SetPrimaryBufferFormat : failed to create the soundbuffer.");
        return dxFailed;
    }

    WAVEFORMATEX wfx;
    ZeroMemory( &wfx, sizeof(WAVEFORMATEX) ); 
    wfx.wFormatTag      = WAVE_FORMAT_PCM; 
    wfx.nChannels       = (WORD) dwPrimaryChannels; 
    wfx.nSamplesPerSec  = dwPrimaryFreq; 
    wfx.wBitsPerSample  = (WORD) dwPrimaryBitRate; 
    wfx.nBlockAlign     = (WORD) (wfx.wBitsPerSample / 8 * wfx.nChannels);
    wfx.nAvgBytesPerSec = wfx.nSamplesPerSec * wfx.nBlockAlign;

    if (FAILED(pDSBPrimary->SetFormat(&wfx)))
    {
        DXCOMMON("(!) SoundManager::SetPrimaryBufferFormat : failed to set format.");
        return dxFailed;
    }

    SAFE_RELEASE( pDSBPrimary );
    return S_OK;
}


/*

//-----------------------------------------------------------------------------
// Name: CSoundManager::Get3DListenerInterface()
// Desc: Returns the 3D listener interface associated with primary buffer.
//-----------------------------------------------------------------------------
Int SoundManager::Get3DListenerInterface( LPDIRECTSOUND3DLISTENER* ppDSListener )
{
    DSBUFFERDESC        dsbdesc;
    LPDIRECTSOUNDBUFFER pDSBPrimary = 0;

    if (ppDSListener == 0)
    {
        DXCOMMON("(!) SoundManager::Get...");
        return E_INVALIDARG;
    }
    if( m_pDS == NULL )
        return CO_E_NOTINITIALIZED;

    *ppDSListener = NULL;

    // Obtain primary buffer, asking it for 3D control
    ZeroMemory( &dsbdesc, sizeof(DSBUFFERDESC) );
    dsbdesc.dwSize = sizeof(DSBUFFERDESC);
    dsbdesc.dwFlags = DSBCAPS_CTRL3D | DSBCAPS_PRIMARYBUFFER;
    if( FAILED( hr = m_pDS->CreateSoundBuffer( &dsbdesc, &pDSBPrimary, NULL ) ) )
        return DXTRACE_ERR( TEXT("CreateSoundBuffer"), hr );

    if( FAILED( hr = pDSBPrimary->QueryInterface( IID_IDirectSound3DListener, 
                                                  (VOID**)ppDSListener ) ) )
    {
        SAFE_RELEASE( pDSBPrimary );
        return DXTRACE_ERR( TEXT("QueryInterface"), hr );
    }

    // Release the primary buffer, since it is not need anymore
    SAFE_RELEASE( pDSBPrimary );

    return S_OK;
}
*/



//-----------------------------------------------------------------------------
// Name: CSoundManager::Create()
// Desc: 
//-----------------------------------------------------------------------------
Int SoundManager::Create( Sound** ppSound, 
                          Char* strWaveFileName, 
                          UInt dwCreationFlags, 
                          GUID guid3DAlgorithm,
                          UInt dwNumBuffers )
{
    HRESULT hres;
    Int     result = dxSuccess;
    UInt   i;
    LPDIRECTSOUNDBUFFER* apDSBuffer     = 0;
    UInt                 dwDSBufferSize = 0;
    WaveFile*            pWaveFile      = 0;

    if (m_pDS == 0)
        return dxFailed;
    if( strWaveFileName == 0 || ppSound == 0 || dwNumBuffers < 1 )
        return dxFailed;

    apDSBuffer = new LPDIRECTSOUNDBUFFER[dwNumBuffers];
    if (apDSBuffer == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Out of memory.");
        // Cleanup
        SAFE_DELETE( pWaveFile );
        SAFE_DELETE( apDSBuffer );
        return dxFailed;
    }

    pWaveFile = new WaveFile();
    if (pWaveFile == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Out of memory.");
        // Cleanup
        SAFE_DELETE( pWaveFile );
        SAFE_DELETE( apDSBuffer );
        return dxFailed;
    }

    pWaveFile->Open( strWaveFileName, NULL, WAVEFILE_READ );

    if (pWaveFile->GetSize() == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Size of Wavefile == 0.");
        // Cleanup
        SAFE_DELETE( pWaveFile );
        SAFE_DELETE( apDSBuffer );
        return dxFailed;
    }

    // Make the DirectSound buffer the same size as the wav file
    dwDSBufferSize = pWaveFile->GetSize();

    // Create the direct sound buffer, and only request the flags needed
    // since each requires some overhead and limits if the buffer can 
    // be hardware accelerated
    DSBUFFERDESC dsbd;
    ZeroMemory( &dsbd, sizeof(DSBUFFERDESC) );
    dsbd.dwSize          = sizeof(DSBUFFERDESC);
    dsbd.dwFlags         = dwCreationFlags;
    dsbd.dwBufferBytes   = dwDSBufferSize;
    dsbd.guid3DAlgorithm = guid3DAlgorithm;
    dsbd.lpwfxFormat     = pWaveFile->m_pwfx;

    // DirectSound is only guarenteed to play PCM data.  Other
    // formats may or may not work depending the sound card driver.
    hres = m_pDS->CreateSoundBuffer( &dsbd, &apDSBuffer[0], NULL );

    // Be sure to return this error code if it occurs so the
    // callers knows this happened.
    if( hres == DS_NO_VIRTUALIZATION )
        result = dxFailed;
            
    if (FAILED(hres))
    {
        // DSERR_BUFFERTOOSMALL will be returned if the buffer is
        // less than DSBSIZE_FX_MIN (100ms) and the buffer is created
        // with DSBCAPS_CTRLFX.
        if (hres != DSERR_BUFFERTOOSMALL)
        {
            DXCOMMON("(!) SoundManager::Create : Buffer too small.");
            // Cleanup
            SAFE_DELETE( pWaveFile );
            SAFE_DELETE( apDSBuffer );
            return dxFailed;
        }
    }

    for (i = 1; i < dwNumBuffers; i++)
    {
        if (FAILED(m_pDS->DuplicateSoundBuffer( apDSBuffer[0], &apDSBuffer[i])))
        {
            DXCOMMON("(!) SoundManager::Create : Error duplicating the soundbuffer for the %dth time.", i);
            // Cleanup
            SAFE_DELETE( pWaveFile );
            SAFE_DELETE( apDSBuffer );
            return dxFailed;
        }
    }

    // Create the sound
    *ppSound = new Sound(apDSBuffer, dwDSBufferSize, dwNumBuffers, pWaveFile);
    
    SAFE_DELETE(apDSBuffer);
    return result;
}









//-----------------------------------------------------------------------------
// Name: CSoundManager::CreateFromMemory()
// Desc: 
//-----------------------------------------------------------------------------
Int SoundManager::CreateFromMemory( Sound** ppSound, 
                                    UByte* pbData,
                                    UInt  ulDataSize,
                                    LPWAVEFORMATEX pwfx,
                                    UInt dwCreationFlags, 
                                    GUID guid3DAlgorithm,
                                    UInt dwNumBuffers )
{
    UInt    i;
    LPDIRECTSOUNDBUFFER* apDSBuffer     = 0;
    DWORD                dwDSBufferSize = 0;
    WaveFile*            pWaveFile      = 0;

    if (m_pDS == 0)
        return dxFailed;
    if (pbData == 0 || ppSound == 0 || dwNumBuffers < 1)
        return dxFailed;

    apDSBuffer = new LPDIRECTSOUNDBUFFER[dwNumBuffers];
    if (apDSBuffer == 0)
    {
        DXCOMMON("(!) SoundManager::CreateFromMemory : Out of memory.");
        SAFE_DELETE(apDSBuffer);
        return dxFailed;
    }

    pWaveFile = new WaveFile();
    if (pWaveFile == 0)
    {
        DXCOMMON("(!) SoundManager::CreateFromMemory : Out of memory.");
        SAFE_DELETE(apDSBuffer);
        return dxFailed;
    }

    pWaveFile->OpenFromMemory((UByte*)pbData,ulDataSize, pwfx, WAVEFILE_READ);


    // Make the DirectSound buffer the same size as the wav file
    dwDSBufferSize = ulDataSize;

    // Create the direct sound buffer, and only request the flags needed
    // since each requires some overhead and limits if the buffer can 
    // be hardware accelerated
    DSBUFFERDESC dsbd;
    ZeroMemory( &dsbd, sizeof(DSBUFFERDESC) );
    dsbd.dwSize          = sizeof(DSBUFFERDESC);
    dsbd.dwFlags         = dwCreationFlags;
    dsbd.dwBufferBytes   = dwDSBufferSize;
    dsbd.guid3DAlgorithm = guid3DAlgorithm;
    dsbd.lpwfxFormat     = pwfx;

    if (FAILED(m_pDS->CreateSoundBuffer(&dsbd, &apDSBuffer[0], 0)))
    {
        DXCOMMON("(!) SoundManager::CreateFromMemory : Error creating SoundBuffer.");
        SAFE_DELETE(apDSBuffer);
        return dxFailed;
    }

    for (i = 1; i < dwNumBuffers; i++)
    {
        if( FAILED(m_pDS->DuplicateSoundBuffer(apDSBuffer[0], &apDSBuffer[i])))
        {
            DXCOMMON("(!) SoundManager::CreateFromMemory : Error duplicating SoundBuffer for the %dth time.",i);
            SAFE_DELETE(apDSBuffer);
            return dxFailed;
        }
    }

    // Create the sound
    *ppSound = new Sound(apDSBuffer, dwDSBufferSize, dwNumBuffers, pWaveFile);

    SAFE_DELETE(apDSBuffer);
    return dxSuccess;
}




/*
//-----------------------------------------------------------------------------
// Name: CSoundManager::CreateStreaming()
// Desc: 
//-----------------------------------------------------------------------------
HRESULT CSoundManager::CreateStreaming( CStreamingSound** ppStreamingSound, 
                                        LPTSTR strWaveFileName, 
                                        DWORD dwCreationFlags, 
                                        GUID guid3DAlgorithm,
                                        DWORD dwNotifyCount, 
                                        DWORD dwNotifySize, 
                                        HANDLE hNotifyEvent )
{
    HRESULT hr;

    if( m_pDS == NULL )
        return CO_E_NOTINITIALIZED;
    if( strWaveFileName == NULL || ppStreamingSound == NULL || hNotifyEvent == NULL )
        return E_INVALIDARG;

    LPDIRECTSOUNDBUFFER pDSBuffer      = NULL;
    DWORD               dwDSBufferSize = NULL;
    CWaveFile*          pWaveFile      = NULL;
    DSBPOSITIONNOTIFY*  aPosNotify     = NULL; 
    LPDIRECTSOUNDNOTIFY pDSNotify      = NULL;

    pWaveFile = new CWaveFile();
    pWaveFile->Open( strWaveFileName, NULL, WAVEFILE_READ );

    // Figure out how big the DSound buffer should be 
    dwDSBufferSize = dwNotifySize * dwNotifyCount;

    // Set up the direct sound buffer.  Request the NOTIFY flag, so
    // that we are notified as the sound buffer plays.  Note, that using this flag
    // may limit the amount of hardware acceleration that can occur. 
    DSBUFFERDESC dsbd;
    ZeroMemory( &dsbd, sizeof(DSBUFFERDESC) );
    dsbd.dwSize          = sizeof(DSBUFFERDESC);
    dsbd.dwFlags         = dwCreationFlags | 
                           DSBCAPS_CTRLPOSITIONNOTIFY | 
                           DSBCAPS_GETCURRENTPOSITION2;
    dsbd.dwBufferBytes   = dwDSBufferSize;
    dsbd.guid3DAlgorithm = guid3DAlgorithm;
    dsbd.lpwfxFormat     = pWaveFile->m_pwfx;

    if( FAILED( hr = m_pDS->CreateSoundBuffer( &dsbd, &pDSBuffer, NULL ) ) )
    {
        // If wave format isn't then it will return 
        // either DSERR_BADFORMAT or E_INVALIDARG
        if( hr == DSERR_BADFORMAT || hr == E_INVALIDARG )
            return DXTRACE_ERR_NOMSGBOX( TEXT("CreateSoundBuffer"), hr );

        return DXTRACE_ERR( TEXT("CreateSoundBuffer"), hr );
    }

    // Create the notification events, so that we know when to fill
    // the buffer as the sound plays. 
    if( FAILED( hr = pDSBuffer->QueryInterface( IID_IDirectSoundNotify, 
                                                (VOID**)&pDSNotify ) ) )
    {
        SAFE_DELETE( aPosNotify );
        return DXTRACE_ERR( TEXT("QueryInterface"), hr );
    }

    aPosNotify = new DSBPOSITIONNOTIFY[ dwNotifyCount ];
    if( aPosNotify == NULL )
        return E_OUTOFMEMORY;

    for( DWORD i = 0; i < dwNotifyCount; i++ )
    {
        aPosNotify[i].dwOffset     = (dwNotifySize * i) + dwNotifySize - 1;
        aPosNotify[i].hEventNotify = hNotifyEvent;             
    }
    
    // Tell DirectSound when to notify us. The notification will come in the from 
    // of signaled events that are handled in WinMain()
    if( FAILED( hr = pDSNotify->SetNotificationPositions( dwNotifyCount, 
                                                          aPosNotify ) ) )
    {
        SAFE_RELEASE( pDSNotify );
        SAFE_DELETE( aPosNotify );
        return DXTRACE_ERR( TEXT("SetNotificationPositions"), hr );
    }

    SAFE_RELEASE( pDSNotify );
    SAFE_DELETE( aPosNotify );

    // Create the sound
    *ppStreamingSound = new CStreamingSound( pDSBuffer, dwDSBufferSize, pWaveFile, dwNotifySize );

    return S_OK;
}
*/



//-----------------------------------------------------------------------------
// Name: CSound::CSound()
// Desc: Constructs the class
//-----------------------------------------------------------------------------
Sound::Sound( LPDIRECTSOUNDBUFFER* apDSBuffer, UInt dwDSBufferSize, 
              UInt dwNumBuffers, WaveFile* pWaveFile )
{
    UInt i;

    m_apDSBuffer = new LPDIRECTSOUNDBUFFER[dwNumBuffers];
    for (i = 0; i < dwNumBuffers; i++)
        m_apDSBuffer[i] = apDSBuffer[i];

    m_dwDSBufferSize = dwDSBufferSize;
    m_dwNumBuffers   = dwNumBuffers;
    m_pWaveFile      = pWaveFile;

    FillBufferWithSound(m_apDSBuffer[0], false);

    // Make DirectSound do pre-processing on sound effects
    for (i = 0; i < dwNumBuffers; i++)
        m_apDSBuffer[i]->SetCurrentPosition(0);
}




//-----------------------------------------------------------------------------
// Name: CSound::~CSound()
// Desc: Destroys the class
//-----------------------------------------------------------------------------
Sound::~Sound()
{
    for (UInt i = 0; i < m_dwNumBuffers; i++)
    {
        SAFE_RELEASE(m_apDSBuffer[i]); 
    }

    SAFE_DELETE_ARRAY(m_apDSBuffer); 
    SAFE_DELETE(m_pWaveFile);
}




//-----------------------------------------------------------------------------
// Name: CSound::FillBufferWithSound()
// Desc: Fills a DirectSound buffer with a sound file 
//-----------------------------------------------------------------------------
Int Sound::FillBufferWithSound(LPDIRECTSOUNDBUFFER pDSB, Boolean bRepeatWavIfBufferLarger)
{
    VOID*   pDSLockedBuffer      = NULL; // Pointer to locked buffer memory
    DWORD   dwDSLockedBufferSize = 0;    // Size of the locked DirectSound buffer
    DWORD   dwWavDataRead        = 0;    // Amount of data read from the wav file 

    if (pDSB == 0)
        return dxFailed;

    // Make sure we have focus, and we didn't just switch in from
    // an app which had a DirectSound device
    if (FAILED(RestoreBuffer(pDSB, 0))) 
    {
        DXCOMMON("(!) Sound::FillBufferWithSound : failed to restore buffer.");
        return dxFailed;
    }

    // Lock the buffer down
    if (FAILED(pDSB->Lock(0, m_dwDSBufferSize, 
                          &pDSLockedBuffer, &dwDSLockedBufferSize, 
                          0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::FillBufferWithSound : failed to lock buffer.");
        return dxFailed;
    }

    // Reset the wave file to the beginning 
    m_pWaveFile->ResetFile();

    if (FAILED(m_pWaveFile->Read((UByte*) pDSLockedBuffer,
                                 dwDSLockedBufferSize, 
                                 (UInt*)&dwWavDataRead)))           
    {
        DXCOMMON("(!) Sound::FillBufferWithSound : failed to read the Wavefile.");
        return dxFailed;
    }

    if (dwWavDataRead == 0)
    {
        // Wav is blank, so just fill with silence
        FillMemory((UByte*) pDSLockedBuffer, 
                   dwDSLockedBufferSize, 
                   (UByte)(m_pWaveFile->m_pwfx->wBitsPerSample == 8 ? 128 : 0 ));
    }
    else if (dwWavDataRead < dwDSLockedBufferSize)
    {
        // If the wav file was smaller than the DirectSound buffer, 
        // we need to fill the remainder of the buffer with data 
        if (bRepeatWavIfBufferLarger)
        {       
            // Reset the file and fill the buffer with wav data
            UInt dwReadSoFar = dwWavDataRead;    // From previous call above.
            while (dwReadSoFar < dwDSLockedBufferSize)
            {  
                // This will keep reading in until the buffer is full 
                // for very short files
                if (FAILED(m_pWaveFile->ResetFile()))
                {
                    DXCOMMON("(!) Sound::FillBufferWithSound : failed to reset file.");
                    return dxFailed;
                }

                if (FAILED(m_pWaveFile->Read((UByte*)pDSLockedBuffer + dwReadSoFar,
                                             dwDSLockedBufferSize - dwReadSoFar,
                                             (UInt*) &dwWavDataRead)))
                {
                    DXCOMMON("(!) Sound::FillBufferWithSound : failed to read Wavefile.");
                    return dxFailed;
                }
                dwReadSoFar += dwWavDataRead;
            } 
        }
        else
        {
            // Don't repeat the wav file, just fill in silence 
            FillMemory((UByte*) pDSLockedBuffer + dwWavDataRead, 
                       dwDSLockedBufferSize - dwWavDataRead, 
                       (UByte)(m_pWaveFile->m_pwfx->wBitsPerSample == 8 ? 128 : 0));
        }
    }

    // Unlock the buffer, we don't need it anymore.
    pDSB->Unlock(pDSLockedBuffer, dwDSLockedBufferSize, 0, 0);

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CSound::RestoreBuffer()
// Desc: Restores the lost buffer. *pbWasRestored returns TRUE if the buffer was 
//       restored.  It can also be NULL if the information is not needed.
//-----------------------------------------------------------------------------
Int Sound::RestoreBuffer(LPDIRECTSOUNDBUFFER pDSB, Boolean* pbWasRestored)
{
    if (pDSB == 0)
        return dxFailed;
    if (pbWasRestored)
        *pbWasRestored = false;

    UInt dwStatus;
    if (FAILED(pDSB->GetStatus((unsigned long*)&dwStatus)))
    {
        DXCOMMON("(!) Sound::RestoreBuffer : failed to retrieve status.");
        return dxFailed;
    }

    if (dwStatus & DSBSTATUS_BUFFERLOST)
    {
        // Since the app could have just been activated, then DirectSound 
        // may not be giving us control yet, so restoring the buffer may fail.  
        // If it does, sleep until DirectSound gives us control.
        HRESULT result;
        do 
        {
            result = pDSB->Restore();
            if (result == DSERR_BUFFERLOST)
                Sleep(10);
        }
        while (result != DS_OK);

        if (pbWasRestored != 0)
            *pbWasRestored = true;
        return dxSuccess;
    }
    else
        return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CSound::GetFreeBuffer()
// Desc: Checks to see if a buffer is playing and returns TRUE if it is.
//-----------------------------------------------------------------------------
LPDIRECTSOUNDBUFFER Sound::GetFreeBuffer()
{
    if (m_apDSBuffer == 0)
        return 0; 

    UInt i;
    for (i = 0; i < m_dwNumBuffers; i++)
    {
        if (m_apDSBuffer[i])
        {  
            DWORD dwStatus = 0;
            m_apDSBuffer[i]->GetStatus( &dwStatus );
            if ((dwStatus & DSBSTATUS_PLAYING ) == 0)
                break;
        }
    }

    if (i != m_dwNumBuffers)
        return m_apDSBuffer[i];
    else
        return m_apDSBuffer[rand() % m_dwNumBuffers];
}




//-----------------------------------------------------------------------------
// Name: CSound::GetBuffer()
// Desc: 
//-----------------------------------------------------------------------------
LPDIRECTSOUNDBUFFER Sound::GetBuffer(UInt dwIndex)
{
    if (m_apDSBuffer == 0)
        return 0;
    if (dwIndex >= m_dwNumBuffers)
        return 0;

    return m_apDSBuffer[dwIndex];
}




//-----------------------------------------------------------------------------
// Name: CSound::Get3DBufferInterface()
// Desc: 
//-----------------------------------------------------------------------------
Int Sound::Get3DBufferInterface(UInt dwIndex, LPDIRECTSOUND3DBUFFER* ppDS3DBuffer)
{
    if (m_apDSBuffer == 0)
        return dxFailed;
    if (dwIndex >= m_dwNumBuffers)
        return dxFailed;

    *ppDS3DBuffer = 0;

    return m_apDSBuffer[dwIndex]->QueryInterface(IID_IDirectSound3DBuffer, 
                                                 (void**)ppDS3DBuffer);
}


//-----------------------------------------------------------------------------
// Name: CSound::Play()
// Desc: Plays the sound using voice management flags.  Pass in DSBPLAY_LOOPING
//       in the dwFlags to loop the sound
//-----------------------------------------------------------------------------
Int Sound::Play(UInt dwPriority, UInt dwFlags)
{
    Boolean  bRestored;

    if (m_apDSBuffer == 0)
        return dxFailed;

    LPDIRECTSOUNDBUFFER pDSB = GetFreeBuffer();

    if (pDSB == 0)
    {
        DXCOMMON("(!) Sound::Play : Failed to get free buffer.");
        return dxFailed;
    }

    // Restore the buffer if it was lost
    if (FAILED(RestoreBuffer(pDSB, &bRestored)))
    {
        DXCOMMON("(!) Sound::Play : Failed to restore buffer.");
        return dxFailed;
    }

    if (bRestored)
    {
        // The buffer was restored, so we need to fill it with new data
        if (FAILED(FillBufferWithSound( pDSB, false)))
        {
            DXCOMMON("(!) Sound::Play : Failed to fill buffer with sound.");
            return dxFailed;
        }

        // Make DirectSound do pre-processing on sound effects
        Reset();
    }

    if (FAILED(pDSB->Play(0, dwPriority, dwFlags)))
        return dxFailed;
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CSound::Stop()
// Desc: Stops the sound from playing
//-----------------------------------------------------------------------------
Int Sound::Stop()
{
    if (m_apDSBuffer == 0)
        return dxFailed;

    HRESULT hr = 0;

    UInt i;
    for (i = 0; i < m_dwNumBuffers; i++)
        hr |= m_apDSBuffer[i]->Stop();
    if (FAILED(hr))
        return dxFailed;
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CSound::Reset()
// Desc: Reset all of the sound buffers
//-----------------------------------------------------------------------------
Int Sound::Reset()
{
    if (m_apDSBuffer == NULL)
        return dxFailed;

    HRESULT hr = 0;

    UInt i;
    for (i = 0; i < m_dwNumBuffers; i++)
        hr |= m_apDSBuffer[i]->SetCurrentPosition(0);
    if (FAILED(hr))
        return dxFailed;
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CSound::IsSoundPlaying()
// Desc: Checks to see if a buffer is playing and returns TRUE if it is.
//-----------------------------------------------------------------------------
Boolean Sound::IsSoundPlaying()
{
    Boolean bIsPlaying = false;

    if (m_apDSBuffer == 0)
        return false; 

    UInt i;
    for (i = 0; i < m_dwNumBuffers; i++)
    {
        if (m_apDSBuffer[i])
        {  
            UInt dwStatus = 0;
            m_apDSBuffer[i]->GetStatus((unsigned long*) &dwStatus);
            bIsPlaying |= ((dwStatus & DSBSTATUS_PLAYING) != 0);
        }
    }

    return bIsPlaying;
}



/*
//-----------------------------------------------------------------------------
// Name: CStreamingSound::CStreamingSound()
// Desc: Setups up a buffer so data can be streamed from the wave file into 
//       buffer.  This is very useful for large wav files that would take a 
//       while to load.  The buffer is initially filled with data, then 
//       as sound is played the notification events are signaled and more data
//       is written into the buffer by calling HandleWaveStreamNotification()
//-----------------------------------------------------------------------------
CStreamingSound::CStreamingSound( LPDIRECTSOUNDBUFFER pDSBuffer, DWORD dwDSBufferSize, 
                                  CWaveFile* pWaveFile, DWORD dwNotifySize ) 
                : CSound( &pDSBuffer, dwDSBufferSize, 1, pWaveFile )           
{
    m_dwLastPlayPos     = 0;
    m_dwPlayProgress    = 0;
    m_dwNotifySize      = dwNotifySize;
    m_dwNextWriteOffset = 0;
    m_bFillNextNotificationWithSilence = FALSE;
}




//-----------------------------------------------------------------------------
// Name: CStreamingSound::~CStreamingSound()
// Desc: Destroys the class
//-----------------------------------------------------------------------------
CStreamingSound::~CStreamingSound()
{
}



//-----------------------------------------------------------------------------
// Name: CStreamingSound::HandleWaveStreamNotification()
// Desc: Handle the notification that tell us to put more wav data in the 
//       circular buffer
//-----------------------------------------------------------------------------
HRESULT CStreamingSound::HandleWaveStreamNotification( BOOL bLoopedPlay )
{
    HRESULT hr;
    DWORD   dwCurrentPlayPos;
    DWORD   dwPlayDelta;
    DWORD   dwBytesWrittenToBuffer;
    VOID*   pDSLockedBuffer = NULL;
    VOID*   pDSLockedBuffer2 = NULL;
    DWORD   dwDSLockedBufferSize;
    DWORD   dwDSLockedBufferSize2;

    if( m_apDSBuffer == NULL || m_pWaveFile == NULL )
        return CO_E_NOTINITIALIZED;

    // Restore the buffer if it was lost
    BOOL bRestored;
    if( FAILED( hr = RestoreBuffer( m_apDSBuffer[0], &bRestored ) ) )
        return DXTRACE_ERR( TEXT("RestoreBuffer"), hr );

    if( bRestored )
    {
        // The buffer was restored, so we need to fill it with new data
        if( FAILED( hr = FillBufferWithSound( m_apDSBuffer[0], FALSE ) ) )
            return DXTRACE_ERR( TEXT("FillBufferWithSound"), hr );
        return S_OK;
    }

    // Lock the DirectSound buffer
    if( FAILED( hr = m_apDSBuffer[0]->Lock( m_dwNextWriteOffset, m_dwNotifySize, 
                                            &pDSLockedBuffer, &dwDSLockedBufferSize, 
                                            &pDSLockedBuffer2, &dwDSLockedBufferSize2, 0L ) ) )
        return DXTRACE_ERR( TEXT("Lock"), hr );

    // m_dwDSBufferSize and m_dwNextWriteOffset are both multiples of m_dwNotifySize, 
    // it should the second buffer should never be valid
    if( pDSLockedBuffer2 != NULL )
        return E_UNEXPECTED; 

    if( !m_bFillNextNotificationWithSilence )
    {
        // Fill the DirectSound buffer with wav data
        if( FAILED( hr = m_pWaveFile->Read( (BYTE*) pDSLockedBuffer, 
                                                  dwDSLockedBufferSize, 
                                                  &dwBytesWrittenToBuffer ) ) )           
            return DXTRACE_ERR( TEXT("Read"), hr );
    }
    else
    {
        // Fill the DirectSound buffer with silence
        FillMemory( pDSLockedBuffer, dwDSLockedBufferSize, 
                    (BYTE)( m_pWaveFile->m_pwfx->wBitsPerSample == 8 ? 128 : 0 ) );
        dwBytesWrittenToBuffer = dwDSLockedBufferSize;
    }

    // If the number of bytes written is less than the 
    // amount we requested, we have a short file.
    if( dwBytesWrittenToBuffer < dwDSLockedBufferSize )
    {
        if( !bLoopedPlay ) 
        {
            // Fill in silence for the rest of the buffer.
            FillMemory( (BYTE*) pDSLockedBuffer + dwBytesWrittenToBuffer, 
                        dwDSLockedBufferSize - dwBytesWrittenToBuffer, 
                        (BYTE)(m_pWaveFile->m_pwfx->wBitsPerSample == 8 ? 128 : 0 ) );

            // Any future notifications should just fill the buffer with silence
            m_bFillNextNotificationWithSilence = TRUE;
        }
        else
        {
            // We are looping, so reset the file and fill the buffer with wav data
            DWORD dwReadSoFar = dwBytesWrittenToBuffer;    // From previous call above.
            while( dwReadSoFar < dwDSLockedBufferSize )
            {  
                // This will keep reading in until the buffer is full (for very short files).
                if( FAILED( hr = m_pWaveFile->ResetFile() ) )
                    return DXTRACE_ERR( TEXT("ResetFile"), hr );

                if( FAILED( hr = m_pWaveFile->Read( (BYTE*)pDSLockedBuffer + dwReadSoFar,
                                                          dwDSLockedBufferSize - dwReadSoFar,
                                                          &dwBytesWrittenToBuffer ) ) )
                    return DXTRACE_ERR( TEXT("Read"), hr );

                dwReadSoFar += dwBytesWrittenToBuffer;
            } 
        } 
    }

    // Unlock the DirectSound buffer
    m_apDSBuffer[0]->Unlock( pDSLockedBuffer, dwDSLockedBufferSize, NULL, 0 );

    // Figure out how much data has been played so far.  When we have played
    // passed the end of the file, we will either need to start filling the
    // buffer with silence or starting reading from the beginning of the file, 
    // depending if the user wants to loop the sound
    if( FAILED( hr = m_apDSBuffer[0]->GetCurrentPosition( &dwCurrentPlayPos, NULL ) ) )
        return DXTRACE_ERR( TEXT("GetCurrentPosition"), hr );

    // Check to see if the position counter looped
    if( dwCurrentPlayPos < m_dwLastPlayPos )
        dwPlayDelta = ( m_dwDSBufferSize - m_dwLastPlayPos ) + dwCurrentPlayPos;
    else
        dwPlayDelta = dwCurrentPlayPos - m_dwLastPlayPos;

    m_dwPlayProgress += dwPlayDelta;
    m_dwLastPlayPos = dwCurrentPlayPos;

    // If we are now filling the buffer with silence, then we have found the end so 
    // check to see if the entire sound has played, if it has then stop the buffer.
    if( m_bFillNextNotificationWithSilence )
    {
        // We don't want to cut off the sound before it's done playing.
        if( m_dwPlayProgress >= m_pWaveFile->GetSize() )
        {
            m_apDSBuffer[0]->Stop();
        }
    }

    // Update where the buffer will lock (for next time)
    m_dwNextWriteOffset += dwDSLockedBufferSize; 
    m_dwNextWriteOffset %= m_dwDSBufferSize; // Circular buffer

    return S_OK;
}




//-----------------------------------------------------------------------------
// Name: CStreamingSound::Reset()
// Desc: Resets the sound so it will begin playing at the beginning
//-----------------------------------------------------------------------------
HRESULT CStreamingSound::Reset()
{
    HRESULT hr;

    if( m_apDSBuffer[0] == NULL || m_pWaveFile == NULL )
        return CO_E_NOTINITIALIZED;

    m_dwLastPlayPos     = 0;
    m_dwPlayProgress    = 0;
    m_dwNextWriteOffset = 0;
    m_bFillNextNotificationWithSilence = FALSE;

    // Restore the buffer if it was lost
    BOOL bRestored;
    if( FAILED( hr = RestoreBuffer( m_apDSBuffer[0], &bRestored ) ) )
        return DXTRACE_ERR( TEXT("RestoreBuffer"), hr );

    if( bRestored )
    {
        // The buffer was restored, so we need to fill it with new data
        if( FAILED( hr = FillBufferWithSound( m_apDSBuffer[0], FALSE ) ) )
            return DXTRACE_ERR( TEXT("FillBufferWithSound"), hr );
    }

    m_pWaveFile->ResetFile();

    return m_apDSBuffer[0]->SetCurrentPosition( 0L );  
}
*/



//-----------------------------------------------------------------------------
// Name: CWaveFile::CWaveFile()
// Desc: Constructs the class.  Call Open() to open a wave file for reading.  
//       Then call Read() as needed.  Calling the destructor or Close() 
//       will close the file.  
//-----------------------------------------------------------------------------
WaveFile::WaveFile()
{
    m_pwfx    = NULL;
    m_hmmio   = NULL;
    m_pResourceBuffer = NULL;
    m_dwSize  = 0;
    m_bIsReadingFromMemory = FALSE;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::~CWaveFile()
// Desc: Destructs the class
//-----------------------------------------------------------------------------
WaveFile::~WaveFile()
{
    Close();

    if (!m_bIsReadingFromMemory)
        SAFE_DELETE_ARRAY( m_pwfx );
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::Open()
// Desc: Opens a wave file for reading
//-----------------------------------------------------------------------------
Int WaveFile::Open(Char* strFileName, WAVEFORMATEX* pwfx, UInt dwFlags)
{
    m_dwFlags = dwFlags;
    m_bIsReadingFromMemory = FALSE;

    if (m_dwFlags == WAVEFILE_READ)
    {
        if (strFileName == 0)
            return dxFailed;
        SAFE_DELETE_ARRAY(m_pwfx);

        m_hmmio = mmioOpen(strFileName, NULL, MMIO_ALLOCBUF | MMIO_READ);

        if (m_hmmio == 0)
        {
            HRSRC   hResInfo;
            HGLOBAL hResData;
            DWORD   dwSize;
            VOID*   pvRes;

            // Loading it as a file failed, so try it as a resource
            if (0 == (hResInfo = FindResource(0, strFileName, TEXT("WAVE"))))
            {
                if (0 == (hResInfo = FindResource(0, strFileName, TEXT("WAV"))))
                {
                    DXCOMMON("(!) WaveFile::Open : failed to locate %s.", strFileName);
                    return dxFailed;
                }
            }

            if (0 == (hResData = LoadResource(0, hResInfo)))
            {
                DXCOMMON("(!) WaveFile::Open : failed to load %s.", strFileName);
                return dxFailed;
            }

            if (0 == (dwSize = SizeofResource(0, hResInfo))) 
            {
                DXCOMMON("(!) WaveFile::Open : failed to retrieve size of %s.", strFileName);
                return dxFailed;
            }

            if (0 == (pvRes = LockResource( hResData )))
            {
                DXCOMMON("(!) WaveFile::Open : failed to lock resource %s.", strFileName);
                return dxFailed;
            }

            m_pResourceBuffer = new Char[dwSize];
            memcpy(m_pResourceBuffer, pvRes, dwSize);

            MMIOINFO mmioInfo;
            ZeroMemory( &mmioInfo, sizeof(mmioInfo) );
            mmioInfo.fccIOProc = FOURCC_MEM;
            mmioInfo.cchBuffer = dwSize;
            mmioInfo.pchBuffer = (Char*) m_pResourceBuffer;

            m_hmmio = mmioOpen(NULL, &mmioInfo, MMIO_ALLOCBUF | MMIO_READ);
        }

        if (FAILED(ReadMMIO()))
        {
            // ReadMMIO will fail if its an not a wave file
            DXCOMMON("(!) WaveFile::Open : failed to read MMIO for %s.", strFileName);
            mmioClose(m_hmmio, 0);
            return dxFailed;
        }

        if (FAILED(ResetFile()))
        {
            DXCOMMON("(!) WaveFile::Open : failed to reset file %s.", strFileName);
            return dxFailed;
        }

        // After the reset, the size of the wav file is m_ck.cksize so store it now
        m_dwSize = m_ck.cksize;
    }
    else
    {
        m_hmmio = mmioOpen(strFileName, NULL, MMIO_ALLOCBUF  | 
                                              MMIO_READWRITE | 
                                              MMIO_CREATE);
        if (0 == m_hmmio)
        {
            DXCOMMON("(!) WaveFile::Open : failed to open MMIO for %s.", strFileName);
            return dxFailed;
        }

        if (FAILED(WriteMMIO(pwfx)))
        {
            mmioClose( m_hmmio, 0 );
            DXCOMMON("(!) WaveFile::Open : failed to write MMIO for %s.", strFileName);
            return dxFailed;
        }
                        
        if (FAILED(ResetFile()))
        {
            DXCOMMON("(!) WaveFile::Open : failed to reset file %s.", strFileName);
            return dxFailed;
        }
    }

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::OpenFromMemory()
// Desc: copy data to CWaveFile member variable from memory
//-----------------------------------------------------------------------------
Int WaveFile::OpenFromMemory(UByte* pbData, UInt ulDataSize, 
                             WAVEFORMATEX* pwfx, UInt dwFlags)
{
    m_pwfx       = pwfx;
    m_ulDataSize = ulDataSize;
    m_pbData     = pbData;
    m_pbDataCur  = m_pbData;
    m_bIsReadingFromMemory = TRUE;
    
    if (dwFlags != WAVEFILE_READ)
        return dxFailed;       
    
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::ReadMMIO()
// Desc: Support function for reading from a multimedia I/O stream.
//       m_hmmio must be valid before calling.  This function uses it to
//       update m_ckRiff, and m_pwfx. 
//-----------------------------------------------------------------------------
Int WaveFile::ReadMMIO()
{
    MMCKINFO        ckIn;           // chunk info. for general use.
    PCMWAVEFORMAT   pcmWaveFormat;  // Temp PCM structure to load in.       

    m_pwfx = NULL;

    if ((0 != mmioDescend(m_hmmio, &m_ckRiff, NULL, 0)))
    {
        DXCOMMON("(!) WaveFile::ReadMMIO : failed to descend to MMIO.");
        return dxFailed;
    }

    // Check to make sure this is a valid wave file
    if ((m_ckRiff.ckid != FOURCC_RIFF) ||
        (m_ckRiff.fccType != mmioFOURCC('W', 'A', 'V', 'E')))
    {
        DXCOMMON("(!) WaveFile::ReadMMIO : invalid wavefile.");
        return dxFailed;
    }

    // Search the input file for for the 'fmt ' chunk.
    ckIn.ckid = mmioFOURCC('f', 'm', 't', ' ');
    if (0 != mmioDescend(m_hmmio, &ckIn, &m_ckRiff, MMIO_FINDCHUNK))
    {
        DXCOMMON("(!) WaveFile::ReadMMIO : failed to descend to MMIO.");
        return dxFailed;
    }

    // Expect the 'fmt' chunk to be at least as large as <PCMWAVEFORMAT>;
    // if there are extra parameters at the end, we'll ignore them
    if (ckIn.cksize < (LONG) sizeof(PCMWAVEFORMAT))
    {
        DXCOMMON("(!) WaveFile::ReadMMIO : 'fmt' chunk too short.");
        return dxFailed;
    }

    // Read the 'fmt ' chunk into <pcmWaveFormat>.
    if (mmioRead(m_hmmio, (HPSTR) &pcmWaveFormat, 
                 sizeof(pcmWaveFormat)) != sizeof(pcmWaveFormat))
    {
        DXCOMMON("(!) WaveFile::ReadMMIO : error reading the actual MMIO.");
        return dxFailed;
    }

    // Allocate the waveformatex, but if its not pcm format, read the next
    // word, and thats how many extra bytes to allocate.
    if (pcmWaveFormat.wf.wFormatTag == WAVE_FORMAT_PCM)
    {
        m_pwfx = (WAVEFORMATEX*)new Char[sizeof(WAVEFORMATEX)];
        if (m_pwfx == 0)
        {
            DXCOMMON("(!) WaveFile::ReadMMIO : failed to allocate memory for waveformat.");
            return dxFailed;
        }

        // Copy the bytes from the pcm structure to the waveformatex structure
        memcpy(m_pwfx, &pcmWaveFormat, sizeof(pcmWaveFormat));
        m_pwfx->cbSize = 0;
    }
    else
    {
        // Read in length of extra bytes.
        UShort cbExtraBytes = 0L;
        if (mmioRead(m_hmmio, (Char*)&cbExtraBytes, sizeof(UShort)) != sizeof(UShort))
        {
            DXCOMMON("(!) WaveFile::ReadMMIO : error reading the actual MMIO.");
            return dxFailed;
        }

        m_pwfx = (WAVEFORMATEX*)new CHAR[ sizeof(WAVEFORMATEX) + cbExtraBytes ];
        if (m_pwfx == 0)
        {
            DXCOMMON("(!) WaveFile::ReadMMIO : failed to allocate memory for waveformat.");
            return dxFailed;
        }

        // Copy the bytes from the pcm structure to the waveformatex structure
        memcpy(m_pwfx, &pcmWaveFormat, sizeof(pcmWaveFormat));
        m_pwfx->cbSize = cbExtraBytes;

        // Now, read those extra bytes into the structure, if cbExtraAlloc != 0.
        if  (mmioRead(m_hmmio, (Char*)(((UByte*)&(m_pwfx->cbSize))+sizeof(UShort)),
                      cbExtraBytes ) != cbExtraBytes)
        {
            SAFE_DELETE(m_pwfx);
            DXCOMMON("(!) WaveFile::ReadMMIO : error reading the actual MMIO.");
            return dxFailed;
        }
    }

    // Ascend the input file out of the 'fmt ' chunk.
    if (0 != mmioAscend( m_hmmio, &ckIn, 0))
    {
        SAFE_DELETE(m_pwfx);
        DXCOMMON("(!) WaveFile::ReadMMIO : error descending to the MMIO.");
        return dxFailed;
    }
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::GetSize()
// Desc: Retuns the size of the read access wave file 
//-----------------------------------------------------------------------------
UInt WaveFile::GetSize()
{
    return m_dwSize;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::ResetFile()
// Desc: Resets the internal m_ck pointer so reading starts from the 
//       beginning of the file again 
//-----------------------------------------------------------------------------
Int WaveFile::ResetFile()
{
    if (m_bIsReadingFromMemory)
    {
        m_pbDataCur = m_pbData;
    }
    else 
    {
        if (m_hmmio == 0)
            return dxFailed;

        if (m_dwFlags == WAVEFILE_READ)
        {
            // Seek to the data
            if (-1 == mmioSeek(m_hmmio, m_ckRiff.dwDataOffset + sizeof(FOURCC),
                               SEEK_SET))
            {
                DXCOMMON("(!) WaveFile::ResetFile : failed to seek MMIO.");
                return dxFailed;
            }

            // Search the input file for the 'data' chunk.
            m_ck.ckid = mmioFOURCC('d', 'a', 't', 'a');
            if (0 != mmioDescend(m_hmmio, &m_ck, &m_ckRiff, MMIO_FINDCHUNK))
            {
                DXCOMMON("(!) WaveFile::ResetFile : failed to descend to MMIO.");
                return dxFailed;
            }
        }
        else
        {
            // Create the 'data' chunk that holds the waveform samples.  
            m_ck.ckid = mmioFOURCC('d', 'a', 't', 'a');
            m_ck.cksize = 0;

            if (0 != mmioCreateChunk(m_hmmio, &m_ck, 0)) 
            {
                DXCOMMON("(!) WaveFile::ResetFile : failed to create data chunk.");
                return dxFailed;
            }

            if (0 != mmioGetInfo(m_hmmio, &m_mmioinfoOut, 0))
            {
                DXCOMMON("(!) WaveFile::ResetFile : failed to retrieve MMIO info.");
                return dxFailed;
            }
        }
    }
    
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::Read()
// Desc: Reads section of data from a wave file into pBuffer and returns 
//       how much read in pdwSizeRead, reading not more than dwSizeToRead.
//       This uses m_ck to determine where to start reading from.  So 
//       subsequent calls will be continue where the last left off unless 
//       Reset() is called.
//-----------------------------------------------------------------------------
Int WaveFile::Read(UByte* pBuffer, UInt dwSizeToRead, UInt* pdwSizeRead)
{
    if (m_bIsReadingFromMemory)
    {
        if (m_pbDataCur == 0)
            return dxFailed;
        if (pdwSizeRead != 0)
            *pdwSizeRead = 0;

        if ((UByte*)(m_pbDataCur + dwSizeToRead) > (UByte*)(m_pbData + m_ulDataSize))
        {
            dwSizeToRead = m_ulDataSize - (UInt)(m_pbDataCur - m_pbData);
        }
        
        CopyMemory(pBuffer, m_pbDataCur, dwSizeToRead);
        
        if (pdwSizeRead != 0)
            *pdwSizeRead = dwSizeToRead;

        return dxSuccess;
    }
    else 
    {
        MMIOINFO mmioinfoIn; // current status of m_hmmio

        if (m_hmmio == 0)
            return dxFailed;
        if (pBuffer == 0 || pdwSizeRead == 0)
            return dxFailed;

        if (pdwSizeRead != 0)
            *pdwSizeRead = 0;

        if (0 != mmioGetInfo(m_hmmio, &mmioinfoIn, 0))
        {
            DXCOMMON("(!)WaveFile::Read : failed to retrieve MMIO info.");
            return dxFailed;
        }

        UInt cbDataIn = dwSizeToRead;
        if (cbDataIn > m_ck.cksize) 
            cbDataIn = m_ck.cksize;       

        m_ck.cksize -= cbDataIn;
    
        for (UInt cT = 0; cT < cbDataIn; cT++)
        {
            // Copy the bytes from the io to the buffer.
            if (mmioinfoIn.pchNext == mmioinfoIn.pchEndRead)
            {
                if (0 != mmioAdvance(m_hmmio, &mmioinfoIn, MMIO_READ))
                {
                    DXCOMMON("(!) WaveFile::Read : failed to advance MMIO.");
                    return dxFailed;
                }

                if (mmioinfoIn.pchNext == mmioinfoIn.pchEndRead)
                    return dxFailed;
            }

            // Actual copy.
            *((UByte*)pBuffer+cT) = *((UByte*)mmioinfoIn.pchNext);
            mmioinfoIn.pchNext++;
        }

        if (0 != mmioSetInfo( m_hmmio, &mmioinfoIn, 0))
        {
            DXCOMMON("(!)WaveFile::Read : error setting MMIO info");
            return dxFailed;
        }

        if (pdwSizeRead != 0)
            *pdwSizeRead = cbDataIn;

        return dxSuccess;
    }
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::Close()
// Desc: Closes the wave file 
//-----------------------------------------------------------------------------
Int WaveFile::Close()
{
    if (m_dwFlags == WAVEFILE_READ)
    {
        mmioClose(m_hmmio, 0);
        m_hmmio = 0;
        SAFE_DELETE_ARRAY(m_pResourceBuffer);
    }
    else
    {
        m_mmioinfoOut.dwFlags |= MMIO_DIRTY;

        if (m_hmmio == NULL)
            return dxFailed;

        if (0 != mmioSetInfo(m_hmmio, &m_mmioinfoOut, 0))
        {
            DXCOMMON("(!) WaveFile::Close : error setting MMIO info.");
            return dxFailed;
        }
    
        // Ascend the output file out of the 'data' chunk -- this will cause
        // the chunk size of the 'data' chunk to be written.
        if (0 != mmioAscend(m_hmmio, &m_ck, 0))
        {
            DXCOMMON("(!) WaveFile::Close : error ascending to the data chunk.");
            return dxFailed;
        }
    
        // Do this here instead...
        if (0 != mmioAscend(m_hmmio, &m_ckRiff, 0))
        {
            DXCOMMON("(!) WaveFile::Close : error ascending to the data chunk.");
            return dxFailed;
        }

        mmioSeek(m_hmmio, 0, SEEK_SET);

        if (0 != (Int)mmioDescend(m_hmmio, &m_ckRiff, 0, 0))
        {
            DXCOMMON("(!) WaveFile::Close : error descending to the data chunk.");
            return dxFailed;
        }

        m_ck.ckid = mmioFOURCC('f', 'a', 'c', 't');

        if (0 == mmioDescend( m_hmmio, &m_ck, &m_ckRiff, MMIO_FINDCHUNK ) ) 
        {
            UInt dwSamples = 0;
            mmioWrite(m_hmmio, (HPSTR)&dwSamples, sizeof(UInt));
            mmioAscend(m_hmmio, &m_ck, 0); 
        }
    
        // Ascend the output file out of the 'RIFF' chunk -- this will cause
        // the chunk size of the 'RIFF' chunk to be written.
        if (0 != mmioAscend(m_hmmio, &m_ckRiff, 0))
        {
            DXCOMMON("(!) WaveFile::Close : error ascending out of RIFF chunk.");
            return dxFailed;
        }

        mmioClose( m_hmmio, 0 );
        m_hmmio = 0;
    }

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::WriteMMIO()
// Desc: Support function for reading from a multimedia I/O stream
//       pwfxDest is the WAVEFORMATEX for this new wave file.  
//       m_hmmio must be valid before calling.  This function uses it to
//       update m_ckRiff, and m_ck.  
//-----------------------------------------------------------------------------
Int WaveFile::WriteMMIO(WAVEFORMATEX *pwfxDest)
{
    UInt    dwFactChunk; // Contains the actual fact chunk. Garbage until WaveCloseWriteFile.
    MMCKINFO ckOut1;
    
    dwFactChunk = (DWORD)-1;

    // Create the output file RIFF chunk of form type 'WAVE'.
    m_ckRiff.fccType = mmioFOURCC('W', 'A', 'V', 'E');       
    m_ckRiff.cksize = 0;

    if (0 != mmioCreateChunk(m_hmmio, &m_ckRiff, MMIO_CREATERIFF))
    {
        DXCOMMON("(!) WaveFile::WriteMMIO : failed to create RIFF chunk.");
        return dxFailed;
    }

    // We are now descended into the 'RIFF' chunk we just created.
    // Now create the 'fmt ' chunk. Since we know the size of this chunk,
    // specify it in the MMCKINFO structure so MMIO doesn't have to seek
    // back and set the chunk size after ascending from the chunk.
    m_ck.ckid = mmioFOURCC('f', 'm', 't', ' ');
    m_ck.cksize = sizeof(PCMWAVEFORMAT);   

    if (0 != mmioCreateChunk( m_hmmio, &m_ck, 0))
    {
        DXCOMMON("(!) WaveFile::WriteMMIO : failed to create the fmt chunk.");
        return dxFailed;
    }

    // Write the PCMWAVEFORMAT structure to the 'fmt ' chunk if its that type. 
    if (pwfxDest->wFormatTag == WAVE_FORMAT_PCM)
    {
        if (mmioWrite(m_hmmio, (HPSTR) pwfxDest, 
                      sizeof(PCMWAVEFORMAT)) != sizeof(PCMWAVEFORMAT))
        {
            DXCOMMON("(!) WaveFile::WriteMMIO : failed to write the fmt chunk.");
            return dxFailed;
        }
    }   
    else 
    {
        // Write the variable length size.
        if ((UInt)mmioWrite( m_hmmio, (HPSTR) pwfxDest, 
                             sizeof(*pwfxDest) + pwfxDest->cbSize ) != 
                             ( sizeof(*pwfxDest) + pwfxDest->cbSize))
        {
            DXCOMMON("(!) WaveFile::WriteMMIO : failed to write the fmt chunk.");
            return dxFailed;
        }
    }  
    
    // Ascend out of the 'fmt ' chunk, back into the 'RIFF' chunk.
    if (0 != mmioAscend(m_hmmio, &m_ck, 0))
    {
        DXCOMMON("(!) WaveFile::WriteMMIO : failed to ascend out of fmt chunk.");
        return dxFailed;
    }

    // Now create the fact chunk, not required for PCM but nice to have.  This is filled
    // in when the close routine is called.
    ckOut1.ckid = mmioFOURCC('f', 'a', 'c', 't');
    ckOut1.cksize = 0;

    if (0 != mmioCreateChunk( m_hmmio, &ckOut1, 0))
    {
        DXCOMMON("(!) WaveFile::WriteMMIO : failed to create fact chunk.");
        return dxFailed;
    }

    if (mmioWrite(m_hmmio, (HPSTR)&dwFactChunk, sizeof(dwFactChunk)) != sizeof(dwFactChunk))
    {
        DXCOMMON("(!) WaveFile::WriteMMIO : failed to write fact chunk.");
        return dxFailed;
    }

    // Now ascend out of the fact chunk...
    if (0 != mmioAscend(m_hmmio, &ckOut1, 0))
    {
        DXCOMMON("(!) WaveFile::WriteMMIO : failed to ascend out of fact chunk.");
        return dxFailed;
    }  

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::Write()
// Desc: Writes data to the open wave file
//-----------------------------------------------------------------------------
Int WaveFile::Write(UInt nSizeToWrite, UByte* pbSrcData, UInt* pnSizeWrote)
{
    UInt cT;

    if (m_bIsReadingFromMemory)
        return dxFailed;
    if (m_hmmio == 0)
        return dxFailed;
    if (pnSizeWrote == 0 || pbSrcData == 0)
        return dxFailed;

    *pnSizeWrote = 0;
    
    for (cT = 0; cT < nSizeToWrite; cT++)
    {       
        if (m_mmioinfoOut.pchNext == m_mmioinfoOut.pchEndWrite)
        {
            m_mmioinfoOut.dwFlags |= MMIO_DIRTY;
            if (0 != mmioAdvance( m_hmmio, &m_mmioinfoOut, MMIO_WRITE))
            {
                DXCOMMON("WafeFile::Write : failed to advance.");
                return dxFailed;
            }
        }

        *((UByte*)m_mmioinfoOut.pchNext) = *((UByte*)pbSrcData+cT);
        (UByte*)m_mmioinfoOut.pchNext++;

        (*pnSizeWrote)++;
    }

    return dxSuccess;
}






}; // namespace DX
