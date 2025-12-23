/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/Common.h>
#include <Common/If/Algorithm.h>  // minimum, maximum
#include <dxerr8.h>



namespace DirectX
{

/*************************************************************************************
 *@class SoundManager
 *@method
 *    constructor
 *@parameters
 *    - handle : a handle to the Window the SoundManager will belong to
 *    - nChannels : the default number of buffers a sound will contain (= number of
 *            times the same sound can be played simultaneously)
 *    - frequency : the default frequency of a soundbuffer
 *    - bitrate : the default bitrate of a soundbuffer
 *************************************************************************************/
SoundManager::SoundManager(::Window::Handle handle, UInt nChannels, UInt frequency, UInt bitrate ) :
    m_created(true),
    m_playInSoftware(false),
    m_reverseStereo(false)
{
	DXCOMMON("(+) SoundManager : %d channels, %d freq, %d bitrate", nChannels, frequency, bitrate);
    m_directSound = 0;
	
    SAFE_RELEASE(m_directSound);
    // Create IDirectSound using the primary sound device
    if (FAILED(DirectSoundCreate8(NULL, &m_directSound, NULL)))
    {
        DXCOMMON("(!) SoundManager : failed to create IDirectSound interface.");        
        m_created = false;
    }

    // Set DirectSound cooperative level 
    if (FAILED(m_directSound->SetCooperativeLevel( handle, DSSCL_PRIORITY)))
    {
        DXCOMMON("(!) SoundManager : failed to set Cooperative level.");
        m_created = false;
    }
         
    // Set primary buffer format
    if (bufferFormat(nChannels, frequency, bitrate) != dxSuccess)
    {
        DXCOMMON("(!) SoundManager : failed to set primary buffer format.");
        m_created = false;
    }

	DWORD mode = 0;
	LPDWORD pdwSpeakerConfig = &mode;

	//get speaker config (stored in mode)
	HRESULT ret = m_directSound->GetSpeakerConfig(pdwSpeakerConfig);

	DXCOMMON("SoundManager : speakerconfig = %d", mode);
}


/*************************************************************************************
 *@class SoundManager
 *@method
 *    destuctor
 *************************************************************************************/
SoundManager::~SoundManager()
{
    DXCOMMON("(-) SoundManager");
    SAFE_RELEASE(m_directSound); 
}



/*************************************************************************************
 *@class SoundManager
 *@method
 *    Int bufferFormat(UInt nChannels, UInt frequency, UInt bitrate)
 *@parameters
 *    - nChannels : the default number of buffers a sound will contain (= number of
 *            times the same sound can be played simultaneously)
 *    - frequency : the default frequency of a soundbuffer
 *    - bitrate : the default bitrate of a soundbuffer
 *
 *@returns
 *    - dxSuccess : if successful
 *    - dxFailed  : otherwise
 *
 *@description
 *    This method modifies the default format of a soundbuffer.
 *************************************************************************************/
Int  
SoundManager::bufferFormat(UInt nChannels, UInt frequency, UInt bitrate)
{
    LPDIRECTSOUNDBUFFER soundbuffer = 0;

    if (m_directSound == 0)
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
       
    if (FAILED(m_directSound->CreateSoundBuffer(&dsbd, &soundbuffer, NULL)))
    {
        DXCOMMON("(!) SoundManager::SetPrimaryBufferFormat : failed to create the soundbuffer.");
        return dxFailed;
    }

    WAVEFORMATEX waveFormat;
    ZeroMemory( &waveFormat, sizeof(WAVEFORMATEX) ); 
    waveFormat.wFormatTag      = WAVE_FORMAT_PCM; 
    waveFormat.nChannels       = (WORD) nChannels; 
    waveFormat.nSamplesPerSec  = frequency; 
    waveFormat.wBitsPerSample  = (WORD) bitrate; 
    waveFormat.nBlockAlign     = (WORD) (waveFormat.wBitsPerSample / 8 * waveFormat.nChannels);
    waveFormat.nAvgBytesPerSec = waveFormat.nSamplesPerSec * waveFormat.nBlockAlign;

    if (FAILED(soundbuffer->SetFormat(&waveFormat)))
    {
        DXCOMMON("(!) SoundManager::SetPrimaryBufferFormat : failed to set format.");
        return dxFailed;
    }

    SAFE_RELEASE(soundbuffer);
    return dxSuccess;
}



Int
SoundManager::listener3DInterface(LPDIRECTSOUND3DLISTENER* listener)
{
    HRESULT             hr;
    DSBUFFERDESC        bufferDesc;
    LPDIRECTSOUNDBUFFER buffer = NULL;

    if (listener == 0)
        return dxFailed;
    if (m_directSound == 0)
        return dxFailed;

    *listener = NULL;

    // Obtain primary buffer, asking it for 3D control
    ZeroMemory( &bufferDesc, sizeof(DSBUFFERDESC) );
    bufferDesc.dwSize = sizeof(DSBUFFERDESC);
    bufferDesc.dwFlags = DSBCAPS_CTRL3D | DSBCAPS_PRIMARYBUFFER;
    if (FAILED( hr = m_directSound->CreateSoundBuffer(&bufferDesc, &buffer, NULL)))
    {
        DXCOMMON("(!) SoundManager::listener3DInterface : CreateSoundBuffer failed.");
        return dxFailed;
    }

    if (FAILED(hr = buffer->QueryInterface(IID_IDirectSound3DListener, (VOID**)listener)))
    {
        SAFE_RELEASE(buffer);
        DXCOMMON("(!) SoundManager::listener3DInterface : QueryInterface failed.");
        return dxFailed;
    }

    // Release the primary buffer, since it is not need anymore
    SAFE_RELEASE(buffer);

    return dxSuccess;
}





Sound* SoundManager::create(Int resource, Boolean enable3d, UInt nBuffers)
{
    Char resourceName[128];
    sprintf(resourceName, "#%d", resource);
    return create(resourceName, enable3d, nBuffers);
}



/*************************************************************************************
 *@class SoundManager
 *@method
 *    Sound* create(Char* filename, UInt nBuffers)
 *@parameters
 *    - filename : the filename of the wavefile you want to create a sound from
 *    - nBuffers : the number of buffers you want the sound to have
 *
 *@returns
 *    A pointer to a sound object if the creation was successful, a 0 pointer 
 *    otherwise.
 *@description
 *    This method of the SoundManager is the reason for it's existence. This method
 *    will perform the necessary actions to load a WaveFile and make a Sound object 
 *    out of it, ready-to-play.    
 *************************************************************************************/
Sound* SoundManager::create(Char* filename, Boolean enable3d, UInt nBuffers)
{
    HRESULT res;
    Int     result = dxSuccess;
    UInt    i;
    LPDIRECTSOUNDBUFFER* buffer     = 0;
    UInt                 bufferSize = 0;
    WaveFile*            waveFile   = 0;
    Sound*               sound      = 0;

    if (m_directSound == 0)
        return 0;
    if (filename == 0 || nBuffers < 1)
        return 0;

    buffer = new LPDIRECTSOUNDBUFFER[nBuffers];
    if (buffer == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Out of memory.");
        // Cleanup
        SAFE_DELETE(buffer);
        return 0;
    }

    waveFile = new WaveFile();
    if (waveFile == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Out of memory.");
        // Cleanup
        SAFE_DELETE(waveFile);
        SAFE_DELETE(buffer);
        return 0;
    }

    waveFile->open(filename, 0, WAVEFILE_READ);

    if (waveFile->size() == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Size of Wavefile == 0.");
        // Cleanup
        SAFE_DELETE(waveFile);
        SAFE_DELETE(buffer);
        return 0;
    }

    // Get the size of the WaveFile so we can set the size of the buffer
    bufferSize = waveFile->size();

    // Create the buffer with the needed flags
    DSBUFFERDESC bufferDesc;
    ZeroMemory(&bufferDesc, sizeof(DSBUFFERDESC));
    bufferDesc.dwSize          = sizeof(DSBUFFERDESC);
    if (enable3d)
        bufferDesc.dwFlags = DSBCAPS_CTRL3D | DSBCAPS_CTRLFREQUENCY;
    else
        bufferDesc.dwFlags = DSBCAPS_GLOBALFOCUS | DSBCAPS_CTRLPAN | DSBCAPS_CTRLVOLUME | DSBCAPS_CTRLFREQUENCY | DSBCAPS_LOCDEFER;
    bufferDesc.dwBufferBytes   = bufferSize;
    if (enable3d)
    {
        switch (m_3dAlgorithm)
        {
        case AlgoDefault:
            bufferDesc.guid3DAlgorithm = GUID_NULL;
            break;
        case AlgoNoVirtualization:
            bufferDesc.guid3DAlgorithm = DS3DALG_NO_VIRTUALIZATION;
            break;
        case AlgoFullHrtf:
            bufferDesc.guid3DAlgorithm = DS3DALG_HRTF_FULL;
            break;
        case AlgoLightHrtf:
            bufferDesc.guid3DAlgorithm = DS3DALG_HRTF_LIGHT;
            break;
        }
    }
    else
        bufferDesc.guid3DAlgorithm = GUID_NULL;
    bufferDesc.lpwfxFormat     = waveFile->m_waveFormat;

    // DirectSound is only guarenteed to play PCM data.  Other
    // formats may or may not work depending the sound card driver.
    res = m_directSound->CreateSoundBuffer( &bufferDesc, &buffer[0], NULL );

    // DS_NO_VIRUTALIZATION is return when hrtf is selected but not available on the system
    //if( res == DS_NO_VIRTUALIZATION )
    //    result = dxFailed;
               
    if (FAILED(res))
    {
        // DSERR_BUFFERTOOSMALL will be returned if the buffer is
        // less than DSBSIZE_FX_MIN (100ms) and the buffer is created
        // with DSBCAPS_CTRLFX.
        if (res != DSERR_BUFFERTOOSMALL)
        {
            DXCOMMON("(!) SoundManager::Create : Error 0x%x creating soundbuffer. (%s)", res, DXGetErrorDescription8(res));
            // Cleanup
            SAFE_DELETE(waveFile);
            SAFE_DELETE(buffer);
            return 0;
        }
    }

    for (i = 1; i < nBuffers; i++)
    {
        if (FAILED(m_directSound->DuplicateSoundBuffer( buffer[0], &buffer[i])))
        {
            DXCOMMON("(!) SoundManager::Create : Error duplicating the soundbuffer for the %dth time.", i);
            // Cleanup
            SAFE_DELETE(waveFile);
            SAFE_DELETE(buffer);
            return 0;
        }
    }

    // Create the sound
    sound = new Sound(buffer, bufferSize, nBuffers, waveFile);
    sound->playInSoftware(m_playInSoftware);
    sound->reverseStereo(m_reverseStereo);
    SAFE_DELETE(buffer);
    return sound;
}

Sound* SoundManager::create(DSBUFFERDESC& bufferDesc, Boolean enable3d, UInt nBuffers)
{
    HRESULT res;
    Int     result = dxSuccess;
    UInt    i;
    LPDIRECTSOUNDBUFFER* buffer     = 0;
    Sound*               sound      = 0;

    if (m_directSound == 0)
        return 0;
    if (nBuffers < 1)
        return 0;

    buffer = new LPDIRECTSOUNDBUFFER[nBuffers];
    if (buffer == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Out of memory.");
        // Cleanup
        SAFE_DELETE(buffer);
        return 0;
    }

    // Create the buffer with the needed flags
    DSBUFFERDESC newBufferDesc = bufferDesc;
//    ZeroMemory(&newBufferDesc, sizeof(DSBUFFERDESC));
    newBufferDesc.dwSize          = sizeof(DSBUFFERDESC);
    if (enable3d)
        newBufferDesc.dwFlags = DSBCAPS_CTRL3D;
    else
        newBufferDesc.dwFlags = DSBCAPS_GLOBALFOCUS | DSBCAPS_CTRLPAN | DSBCAPS_CTRLVOLUME | DSBCAPS_CTRLFREQUENCY | DSBCAPS_LOCDEFER;
    if (enable3d)
    {
        switch (m_3dAlgorithm)
        {
        case AlgoDefault:
            newBufferDesc.guid3DAlgorithm = GUID_NULL;
            break;
        case AlgoNoVirtualization:
            newBufferDesc.guid3DAlgorithm = DS3DALG_NO_VIRTUALIZATION;
            break;
        case AlgoFullHrtf:
            newBufferDesc.guid3DAlgorithm = DS3DALG_HRTF_FULL;
            break;
        case AlgoLightHrtf:
            newBufferDesc.guid3DAlgorithm = DS3DALG_HRTF_LIGHT;
            break;
        }
    }
    else
        newBufferDesc.guid3DAlgorithm = GUID_NULL;

    // DirectSound is only guarenteed to play PCM data.  Other
    // formats may or may not work depending the sound card driver.
    res = m_directSound->CreateSoundBuffer( &newBufferDesc, &buffer[0], NULL );

    // DS_NO_VIRUTALIZATION is return when hrtf is selected but not available on the system
    //if( res == DS_NO_VIRTUALIZATION )
    //    result = dxFailed;
               
    if (FAILED(res))
    {
        // DSERR_BUFFERTOOSMALL will be returned if the buffer is
        // less than DSBSIZE_FX_MIN (100ms) and the buffer is created
        // with DSBCAPS_CTRLFX.
        if (res != DSERR_BUFFERTOOSMALL)
        {
            DXCOMMON("(!) SoundManager::Create : Error 0x%x creating soundbuffer. (%s)", res, DXGetErrorDescription8(res));
            // Cleanup
            SAFE_DELETE(buffer);
            return 0;
        }
    }

    for (i = 1; i < nBuffers; i++)
    {
        if (FAILED(m_directSound->DuplicateSoundBuffer( buffer[0], &buffer[i])))
        {
            DXCOMMON("(!) SoundManager::Create : Error duplicating the soundbuffer for the %dth time.", i);
            // Cleanup
            SAFE_DELETE(buffer);
            return 0;
        }
    }

    // Create the sound
    sound = new Sound(buffer, newBufferDesc.dwBufferBytes, nBuffers, newBufferDesc.lpwfxFormat);
    sound->playInSoftware(m_playInSoftware);
    sound->reverseStereo(m_reverseStereo);
    SAFE_DELETE(buffer);
    return sound;
}



#ifdef _USE_VORBIS_
Sound* SoundManager::createVorbis(Char* filename, Boolean enable3d, UInt nBuffers)
{
    HRESULT res;
    Int     result = dxSuccess;
    UInt    i;
    LPDIRECTSOUNDBUFFER* buffer     = 0;
    UInt                 bufferSize = 0;
    WaveFile*            waveFile   = 0;
    Sound*               sound      = 0;

    if (m_directSound == 0)
        return 0;
    if (filename == 0 || nBuffers < 1)
        return 0;

    buffer = new LPDIRECTSOUNDBUFFER[nBuffers];
    if (buffer == 0)
    {
        DXCOMMON("(!) SoundManager::Create : Out of memory.");
        // Cleanup
        SAFE_DELETE(buffer);
        return 0;
    }

    // Ogg vorbis specific code
    FILE *file;
    file = fopen(filename, "rb");
    if (file == 0)
    {
        DXCOMMON("(!) SoundManager::Create : could not locate %s", filename);
        // Cleanup
        SAFE_DELETE(buffer);
        return 0;
    }
    OggVorbis_File vorbisFile;
    ov_open(file, &vorbisFile, NULL, 0);

    vorbis_info *vi = ov_info(&vorbisFile, -1);

    // Get the size of the VorbisFile so we can set the size of the buffer
    bufferSize = (UInt) ov_pcm_total(&vorbisFile, -1) * 2 * vi->channels;

    // Get the wave format
    WAVEFORMATEX        waveFormat;
    ZeroMemory(&waveFormat, sizeof(WAVEFORMATEX));

    waveFormat.cbSize           = sizeof(WAVEFORMATEX);
    waveFormat.nChannels        = vi->channels;
    waveFormat.wBitsPerSample   = 16; // vorbis is always 16
    waveFormat.nSamplesPerSec   = vi->rate;
    waveFormat.nAvgBytesPerSec  = waveFormat.nSamplesPerSec*waveFormat.nChannels*2;
    waveFormat.nBlockAlign      = 2*waveFormat.nChannels;
    waveFormat.wFormatTag       = 1;

    // Create the buffer with the needed flags
    DSBUFFERDESC bufferDesc;
    ZeroMemory(&bufferDesc, sizeof(DSBUFFERDESC));
    bufferDesc.dwSize          = sizeof(DSBUFFERDESC);
    if (enable3d)
        bufferDesc.dwFlags = DSBCAPS_CTRL3D;
    else
        bufferDesc.dwFlags         = DSBCAPS_GLOBALFOCUS | DSBCAPS_CTRLPAN | DSBCAPS_CTRLVOLUME | DSBCAPS_CTRLFREQUENCY | DSBCAPS_LOCDEFER;
    bufferDesc.dwBufferBytes   = bufferSize;
    bufferDesc.guid3DAlgorithm = GUID_NULL;
    bufferDesc.lpwfxFormat     = &waveFormat;

    // DirectSound is only guarenteed to play PCM data.  Other
    // formats may or may not work depending the sound card driver.
    res = m_directSound->CreateSoundBuffer( &bufferDesc, &buffer[0], NULL );

    // Be sure to return this error code if it occurs so the
    // callers knows this happened.
    if( res == DS_NO_VIRTUALIZATION )
        result = dxFailed;
            
    if (FAILED(res))
    {
        // DSERR_BUFFERTOOSMALL will be returned if the buffer is
        // less than DSBSIZE_FX_MIN (100ms) and the buffer is created
        // with DSBCAPS_CTRLFX.
        if (res != DSERR_BUFFERTOOSMALL)
        {
            DXCOMMON("(!) SoundManager::Create : Error 0x%x creating soundbuffer. (%s)", res, DXGetErrorDescription8(res));
            // Cleanup
            SAFE_DELETE(waveFile);
            SAFE_DELETE(buffer);
            return 0;
        }
    }

    for (i = 1; i < nBuffers; i++)
    {
        if (FAILED(m_directSound->DuplicateSoundBuffer( buffer[0], &buffer[i])))
        {
            DXCOMMON("(!) SoundManager::Create : Error duplicating the soundbuffer for the %dth time.", i);
            // Cleanup
            SAFE_DELETE(waveFile);
            SAFE_DELETE(buffer);
            return 0;
        }
    }

    // Create the sound
    sound = new Sound(buffer, bufferSize, nBuffers, &vorbisFile, 
                      waveFormat.wBitsPerSample, waveFormat.nAvgBytesPerSec);
   
    sound->playInSoftware(m_playInSoftware);
    sound->reverseStereo(m_reverseStereo);
    SAFE_DELETE(buffer);
    ov_clear(&vorbisFile);
    fclose(file);
    return sound;
}
#endif




/*************************************************************************************
 *@class Sound
 *@method
 *    constuctor
 *************************************************************************************/
Sound::Sound( LPDIRECTSOUNDBUFFER* buffer, UInt bufferSize, 
             UInt nBuffers, WaveFile* waveFile ) :
    m_buffer3D(0)
{
    UInt i;

    m_buffer = new LPDIRECTSOUNDBUFFER[nBuffers];
    for (i = 0; i < nBuffers; i++)
        m_buffer[i] = buffer[i];

    m_bufferSize = bufferSize;
    m_nBuffers   = nBuffers;
    m_waveFile   = waveFile;
    m_playInSoftware = false;
    m_reverseStereo = 1;

    fillBufferWithSound(m_buffer[0]);

    // calculate the length of the sound
    m_length = ((m_bufferSize*1.0f)/m_waveFile->m_waveFormat->nAvgBytesPerSec);

    // Rewind all buffers
    for (i = 0; i < nBuffers; i++)
        m_buffer[i]->SetCurrentPosition(0);
}


Sound::Sound(LPDIRECTSOUNDBUFFER* buffer, UInt bufferSize, UInt nBuffers, LPWAVEFORMATEX waveFormat) :
    m_buffer3D(0)
{
    UInt i;

    m_buffer = new LPDIRECTSOUNDBUFFER[nBuffers];
    for (i = 0; i < nBuffers; i++)
        m_buffer[i] = buffer[i];

    m_bufferSize = bufferSize;
    m_nBuffers   = nBuffers;
    m_waveFile   = 0;
    m_playInSoftware = false;
    m_reverseStereo = 1;

    //fillBufferWithSilence(m_buffer[0], waveFormat);

    // calculate the length of the sound
    m_length = ((m_bufferSize*1.0f)/waveFormat->nAvgBytesPerSec);

    // Rewind all buffers
    for (i = 0; i < nBuffers; i++)
        m_buffer[i]->SetCurrentPosition(0);
}



#ifdef _USE_VORBIS_
Sound::Sound(LPDIRECTSOUNDBUFFER* buffer, UInt bufferSize, UInt nBuffers,
             OggVorbis_File* vorbisFile, UShort bitsPerSample, UInt avgBytesPerSec) :
    m_buffer3D(0)
{
    UInt i;

    m_buffer = new LPDIRECTSOUNDBUFFER[nBuffers];
    for (i = 0; i < nBuffers; i++)
        m_buffer[i] = buffer[i];

    m_bufferSize = bufferSize;
    m_nBuffers   = nBuffers;
    m_waveFile   = 0;
    m_playInSoftware = false;
    m_reverseStereo = 1;

    fillBufferWithSound(m_buffer[0], vorbisFile, bitsPerSample);

    // calculate the length of the sound
    m_length = ((m_bufferSize*1.0f)/avgBytesPerSec);

    // Rewind all buffers
    for (i = 0; i < nBuffers; i++)
        m_buffer[i]->SetCurrentPosition(0);
}
#endif


/*************************************************************************************
 *@class Sound
 *@method
 *    destructor
 *************************************************************************************/
Sound::~Sound()
{
    if (playing())
        stop( );
    for (UInt i = 0; i < m_nBuffers; i++)
    {
        SAFE_RELEASE(m_buffer[i]); 
    }

    SAFE_DELETE_ARRAY(m_buffer); 
    SAFE_DELETE(m_waveFile);
}




/*************************************************************************************
 *@class Sound
 *@method 
 *    Int fillBufferWithSound(LPDIRECTSOUNDBUFFER buffer)
 *@parameters
 *    - buffer : the buffer you want filled
 *
 *@description
 *    As the name says, this method fills the passed buffer with the sound from the
 *    WafeFile associated with this Sound.
 *************************************************************************************/
Int 
Sound::fillBufferWithSound(LPDIRECTSOUNDBUFFER buffer)
{
    void*   lockedBuffer     = 0; // Pointer to locked buffer memory
    UInt    lockedBufferSize = 0;    // Size of the locked DirectSound buffer
    UInt    nWavDataRead     = 0;    // Amount of data read from the wav file 

    if (buffer == 0)
        return dxFailed;

    // Make sure we have focus, and we didn't just switch in from
    // an app which had a DirectSound device
    if (FAILED(restoreBuffer(buffer, 0))) 
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to restore buffer.");
        return dxFailed;
    }

    // Lock the buffer down
    if (FAILED(buffer->Lock(0, m_bufferSize, 
                            &lockedBuffer, (unsigned long*) &lockedBufferSize, 
                            0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to lock buffer.");
        return dxFailed;
    }

    // Reset the wave file to the beginning 
    m_waveFile->resetFile();

    if (FAILED(m_waveFile->read((UByte*) lockedBuffer,
                                 lockedBufferSize, 
                                 (UInt*)&nWavDataRead)))           
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to read the Wavefile.");
        return dxFailed;
    }

    if (nWavDataRead == 0)
    {
        // Wav is blank, so just fill with silence
        FillMemory((UByte*) lockedBuffer, 
                   lockedBufferSize, 
                   (UByte)(m_waveFile->m_waveFormat->wBitsPerSample == 8 ? 128 : 0 ));
    }
    else if (nWavDataRead < lockedBufferSize)
    {
        FillMemory((UByte*) lockedBuffer + nWavDataRead, 
                    lockedBufferSize - nWavDataRead, 
                    (UByte)(m_waveFile->m_waveFormat->wBitsPerSample == 8 ? 128 : 0));
    }

    // Unlock the buffer, we don't need it anymore.
    buffer->Unlock(lockedBuffer, lockedBufferSize, 0, 0);

    return dxSuccess;
}


Int 
Sound::fillBufferWithSilence(LPDIRECTSOUNDBUFFER buffer, LPWAVEFORMATEX waveFormat)
{
    void*   lockedBuffer     = 0; // Pointer to locked buffer memory
    UInt    lockedBufferSize = 0;    // Size of the locked DirectSound buffer
    UInt    nWavDataRead     = 0;    // Amount of data read from the wav file 

    if (buffer == 0)
        return dxFailed;

    // Make sure we have focus, and we didn't just switch in from
    // an app which had a DirectSound device
    if (FAILED(restoreBuffer(buffer, 0))) 
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to restore buffer.");
        return dxFailed;
    }

    // Lock the buffer down
    if (FAILED(buffer->Lock(0, m_bufferSize, 
                            &lockedBuffer, (unsigned long*) &lockedBufferSize, 
                            0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to lock buffer.");
        return dxFailed;
    }


    FillMemory((UByte*) lockedBuffer,
               lockedBufferSize,
               (UByte)(waveFormat->wBitsPerSample == 8 ? 128 : 0));
/*
    static i = 0;
    i += 5;
    FillMemory((UByte*) lockedBuffer,
               lockedBufferSize,
               (UByte)(i % 256));
*/

    // Unlock the buffer, we don't need it anymore.
    buffer->Unlock(lockedBuffer, lockedBufferSize, 0, 0);

    return dxSuccess;
}

#ifdef _USE_VORBIS_
Int Sound::fillBufferWithSound(LPDIRECTSOUNDBUFFER buffer, OggVorbis_File *vorbisFile, UShort bitsPerSample)
{
    void*   lockedBuffer     = 0; // Pointer to locked buffer memory
    UInt    lockedBufferSize = 0;    // Size of the locked DirectSound buffer

    if (buffer == 0)
        return dxFailed;

    // Make sure we have focus, and we didn't just switch in from
    // an app which had a DirectSound device
    if (FAILED(restoreBuffer(buffer, 0))) 
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to restore buffer.");
        return dxFailed;
    }

    // Lock the buffer down
    if (FAILED(buffer->Lock(0, m_bufferSize, 
                            &lockedBuffer, (unsigned long*) &lockedBufferSize, 
                            0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::fillBufferWithSound : failed to lock buffer.");
        return dxFailed;
    }

    // read in the data
    UInt size = lockedBufferSize;
    int retval = 1;
    int pos = 0;
    int sec = 0;
    while (retval && pos < (int)size)
    {
        retval = ov_read(vorbisFile, (char*)(lockedBuffer) + pos, size - pos, 0, 2, 1, &sec);
        pos += retval;
    }

    // fill the remainder with silence
    if (pos == 0)
    {
        // Wav is blank, so just fill with silence
        FillMemory((UByte*) lockedBuffer, 
                   lockedBufferSize, 
                   (UByte)(bitsPerSample == 8 ? 128 : 0 ));
    }
    else if ((UInt)pos < lockedBufferSize)
    {
        FillMemory((UByte*) lockedBuffer + pos, 
                    lockedBufferSize - pos, 
                    (UByte)(bitsPerSample == 8 ? 128 : 0));
    }

    // Unlock the buffer, we don't need it anymore.
    buffer->Unlock(lockedBuffer, lockedBufferSize, 0, 0);

    return dxSuccess;
}
#endif


/*************************************************************************************
 *@class Sound
 *@method
 *    Int restoreBuffer(LPDIRECTSOUNDBUFFER buffer, Boolean* wasRestored)
 *************************************************************************************/

Int Sound::restoreBuffer(LPDIRECTSOUNDBUFFER buffer, Boolean* wasRestored)
{
    if (buffer == 0)
        return dxFailed;
    if (wasRestored)
        *wasRestored = false;

    UInt status;
    if (FAILED(buffer->GetStatus((unsigned long*)&status)))
    {
        DXCOMMON("(!) Sound::restoreBuffer : failed to retrieve status.");
        return dxFailed;
    }

    if (status & DSBSTATUS_BUFFERLOST)
    {
        // Wait until we get control
        HRESULT result;
        do 
        {
            result = buffer->Restore();
            if (result == DSERR_BUFFERLOST)
                Sleep(10);
        }
        while (result != DS_OK);

        if (wasRestored != 0)
            *wasRestored = true;
        return dxSuccess;
    }
    else
        return dxSuccess;
}




/*************************************************************************************
 *@Sound
 *@method
 *    LPDIRECTSOUNDBUFFER getFreeBuffer()
 *@returns
 *    A free SoundBuffer associated with this Sound, or, when none being available, a
 *    random one.
 *************************************************************************************/
LPDIRECTSOUNDBUFFER Sound::getFreeBuffer()
{
    if (m_buffer == 0)
        return 0; 

    UInt i;
    for (i = 0; i < m_nBuffers; i++)
    {
        if (m_buffer[i])
        {  
            ULong status = 0;
            m_buffer[i]->GetStatus(&status);
            if ((status & DSBSTATUS_PLAYING ) == 0)
                break;
        }
    }

    if (i != m_nBuffers)
        return m_buffer[i];
    else
        return m_buffer[rand() % m_nBuffers];
}




/*************************************************************************************
 *@class Sound
 *@method 
 *    Int play(UInt priority, UInt flags)
 *@parameters
 *    - priority : indicates the priority of this sound
 *    - looped ; wether or not the sound will be played contineously
 *
 *@returns
 *    - dxSuccess : if successfull
 *    - dxFailed : otherwise
 *
 *************************************************************************************/
Int Sound::play(UInt priority, Boolean looped)
{
    Boolean  restored;

    if (m_buffer == 0)
        return dxFailed;

    LPDIRECTSOUNDBUFFER buffer = getFreeBuffer();

    if (buffer == 0)
    {
        DXCOMMON("(!) Sound::Play : Failed to get free buffer.");
        return dxFailed;
    }

    if (FAILED(restoreBuffer(buffer, &restored)))
    {
        DXCOMMON("(!) Sound::Play : Failed to restore buffer.");
        return dxFailed;
    }

    if (restored)
    {
        // The buffer was restored, so we need to fill it with new data
        if (FAILED(fillBufferWithSound(buffer)))
        {
            DXCOMMON("(!) Sound::Play : Failed to fill buffer with sound.");
            return dxFailed;
        }

        // Make DirectSound do pre-processing on sound effects
        reset();
    }

    // Set the loop flag if necessary
    UInt flags = 0;
    if (m_playInSoftware)
        flags |= DSBPLAY_LOCSOFTWARE;
    if (looped)
        flags |= DSBPLAY_LOOPING;

    HRESULT hr;
    if (FAILED(hr = buffer->Play(0, priority, flags)))
    {
        if (hr == DSERR_CONTROLUNAVAIL)
            DXCOMMON("(!) Sound::Play : DSERR_CONTROLUNAVAIL");
        else if (hr == DSERR_INVALIDCALL)
            DXCOMMON("(!) Sound::Play : DSERR_INVALIDCALL");
        else if (hr == E_FAIL)
            DXCOMMON("(!) Sound::Play : E_FAIL");
        else if (hr == E_NOTIMPL)
            DXCOMMON("(!) Sound::Play : E_NOTIMPL");
        else if (hr == DSERR_BUFFERLOST)
            DXCOMMON("(!) Sound::Play : DSERR_BUFFERLOST");
        else if (hr == DSERR_PRIOLEVELNEEDED)
            DXCOMMON("(!) Sound::Play : DSERR_PRIOLEVELNEEDED");
        else
            DXCOMMON("(!) Sound::Play : unknown error");
        return dxFailed;
    }

    // If arrived here, the sound is playing!
    return dxSuccess;
}




/*************************************************************************************
 *@class Sound
 *@method
 *    Int stop( )
 *@returns
 *    - dxSuccess : if successfull
 *    - dxFailed  : otherwise
 *
 *@description
 *    Should this be explained?
 *************************************************************************************/
Int Sound::stop()
{
    if (m_buffer == 0)
        return dxFailed;

    HRESULT hr = 0;

    UInt i;
    for (i = 0; i < m_nBuffers; i++)
        hr |= m_buffer[i]->Stop();
    if (FAILED(hr))
        return dxFailed;
    return dxSuccess;
}




/*************************************************************************************
 *@class Sound
 *@method
 *    Int reset( )
 *@returns
 *    - dxSuccess : if successfull
 *    - dxFailed  : otherwise
 *
 *@description
 *    Sets all buffer read pointers to the beginning.
 *************************************************************************************/
Int Sound::reset()
{
    if (m_buffer == 0)
        return dxFailed;

    HRESULT result = 0;

    UInt i;
    for (i = 0; i < m_nBuffers; i++)
        result |= m_buffer[i]->SetCurrentPosition(0);
    if (FAILED(result))
        return dxFailed;
    return dxSuccess;
}




/*************************************************************************************
 *@class Sound
 *@method 
 *    Boolean playing( )
 *
 *@returns
 *    - true : when the sound is being played
 *    - false : otherwise
 *
 *************************************************************************************/
Boolean Sound::playing()
{
    if (m_buffer == 0)
        return false; 

    Boolean playing = false;
    UInt i;
    for (i = 0; i < m_nBuffers; i++)
    {
        if (m_buffer[i])
        {  
            UInt dwStatus = 0;
            m_buffer[i]->GetStatus((unsigned long*) &dwStatus);
            playing |= ((dwStatus & DSBSTATUS_PLAYING) != 0);
        }
    }

    return playing;
}



Int Sound::initializeBuffer3D(UInt index)
{
    if (m_buffer3D)
    {
        DXCOMMON("(!) Sound::initializeBuffer3D : already initialized");
        return dxFailed;
    }
    if (index >= m_nBuffers)
    {
        DXCOMMON("(!) Sound::InitializeBuffer3D : index out of range");
        return dxFailed;
    }
    if (m_buffer[index] == 0)
    {
        DXCOMMON("(!) Sound::InitializeBuffer3D : buffer not initialized");
        return dxFailed;
    }

    m_buffer3D = 0;

    HRESULT hr;
    if (FAILED(hr = m_buffer[index]->QueryInterface(IID_IDirectSound3DBuffer, 
                                                   (VOID**)&m_buffer3D)))
    {
        DXCOMMON("(!) Sound::InitializeBuffer3D : QueryInterface failed.");
        return dxFailed;
    }

    m_parameters.dwSize = sizeof(DS3DBUFFER);
    m_buffer3D->GetAllParameters(&m_parameters);
    return dxSuccess;
}



/*************************************************************************************
 *@class Sound
 *@method 
 *    void pan(Int value)
 *@parameters
 *    - value : an integer value ranging from -100 to +100 indicating the amount of
 *         pan to apply (negative values meaning pan to the left).
 *
 *************************************************************************************/
void Sound::pan(Int value)
{   
    UInt i;
    if (value == 0)
    {
        for (i = 0; i < m_nBuffers; ++i)
            if (m_buffer[i])
                m_buffer[i]->SetPan(DSBPAN_CENTER);
    }
    else if (value > 0)
    {
        for (i = 0; i < m_nBuffers; ++i)
            if (m_buffer[i])
                m_buffer[i]->SetPan(minimum<Int>(value, 100)* m_reverseStereo * DSBPAN_RIGHT/100);
                //m_buffer[i]->SetPan(value*6);
    }
    else if (value < 0)
    {
        for (i = 0; i < m_nBuffers; ++i)
            if (m_buffer[i])
                m_buffer[i]->SetPan(maximum<Int>(value, -100)* m_reverseStereo * DSBPAN_LEFT/-100);
                //m_buffer[i]->SetPan(value*6);
    }
}



/*************************************************************************************
 *@class Sound
 *@method
 *    void frequency(Int value)
 *@parameters
 *    - value : the frequency the sound should be played at. Playing sounds at higher
 *          frequencies also speeds them up.
 *
 *************************************************************************************/
void Sound::frequency(Int value)
{
    UInt i;
    if (value < DSBFREQUENCY_MIN)
    {
        for (i = 0; i < m_nBuffers; ++i)
            if (m_buffer[i])
                m_buffer[i]->SetFrequency(DSBFREQUENCY_MIN);
    }
    else if (value > DSBFREQUENCY_MAX)
    {
        for (i = 0; i < m_nBuffers; ++i)
            if (m_buffer[i])
                m_buffer[i]->SetFrequency(DSBFREQUENCY_MAX);
    }
    else
    {
        for (i = 0; i < m_nBuffers; ++i)
            if (m_buffer[i])
                m_buffer[i]->SetFrequency(value);
    }
}


/*************************************************************************************
 *@
 *@
 *
 *************************************************************************************/
UInt Sound::frequency( )
{
    DWORD freq;
    m_buffer[0]->GetFrequency(&freq);
    return (UInt) freq;
}


/*************************************************************************************
 *@class Sound
 *@method
 *    void volume(Int value)
 *@parameters
 *    - value : an integer value ranging from 0 (mute) to 100 (full volume) indicating
 *        the volume the sound should be played at.
 *
 *************************************************************************************/
void Sound::volume(Int value)
{
    UInt i;
    if (value < 0)
    {
        for (i = 0; i < m_nBuffers; i++)
            if (m_buffer[i])
                m_buffer[i]->SetVolume(DSBVOLUME_MIN);
    }
    else if (value > 100)
    {
        for (i = 0; i < m_nBuffers; i++)
            if (m_buffer[i])
                m_buffer[i]->SetVolume(DSBVOLUME_MAX);
    }
    else
    {
        for (i = 0; i < m_nBuffers; i++)
            if (m_buffer[i])
                m_buffer[i]->SetVolume(DSBVOLUME_MIN + value*(DSBVOLUME_MAX - DSBVOLUME_MIN)/100);
    }
}



Float 
Sound::length( )
{
    return m_length;
}

void 
Sound::position(Vector3 pos)
{
    DWORD applyFlag = DS3D_IMMEDIATE;
    m_parameters.vPosition.x = pos.x;
    m_parameters.vPosition.y = pos.y;
    m_parameters.vPosition.z = pos.z;
    
    m_buffer3D->SetPosition(pos.x, pos.y, pos.z, applyFlag);
}

UInt
Sound::bufferSize( )
{ 
    return m_bufferSize;  
}

WAVEFORMATEX*
Sound::waveFormat( )
{ 
    return m_waveFile->waveFormat( );
}


UInt
Sound::copyBuffer(LPDIRECTSOUNDBUFFER* buffer, UInt bufferOffset, UInt bufferSize)
{
    void*   lockedBuffer     = 0; // Pointer to locked buffer memory
    UInt    lockedBufferSize = 0;    // Size of the locked DirectSound buffer
    void*   lockedOriginalBuffer = 0;
    UInt    lockedOriginalBufferSize = 0;


    if (buffer == 0)
        return dxFailed;

    // Make sure we have focus, and we didn't just switch in from
    // an app which had a DirectSound device
    if (FAILED(restoreBuffer(*m_buffer, 0))) 
    {
        DXCOMMON("(!) Sound::copyBuffer : failed to restore buffer.");
        return dxFailed;
    }

    // Lock the buffer down
    if (FAILED((*m_buffer)->Lock(0, m_bufferSize, 
                            &lockedBuffer, (unsigned long*) &lockedBufferSize, 
                            0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::copyBuffer : failed to lock buffer.");
        return dxFailed;
    }

    // Lock the original buffer down
    if (FAILED((*buffer)->Lock(0, bufferSize,
                            &lockedOriginalBuffer, (unsigned long*) &lockedOriginalBufferSize,
                            0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::copyBuffer : failed to lock original buffer.");
        return dxFailed;
    }



    CopyMemory(((UByte*)lockedBuffer)+bufferOffset, lockedOriginalBuffer, bufferSize);

    // Unlock the buffer, we don't need it anymore.
    (*m_buffer)->Unlock(lockedBuffer, lockedBufferSize, 0, 0);
    (*buffer)->Unlock(lockedOriginalBuffer, lockedOriginalBufferSize, 0, 0);

    return dxSuccess;
}


UInt
Sound::insertSilence(UInt bufferOffset, UInt bufferSize)
{
    void*   lockedBuffer     = 0; // Pointer to locked buffer memory
    UInt    lockedBufferSize = 0;    // Size of the locked DirectSound buffer

    if ((*m_buffer) == 0) // Remember this spot
        return dxFailed;

    // Make sure we have focus, and we didn't just switch in from
    // an app which had a DirectSound device
    if (FAILED(restoreBuffer(*m_buffer, 0))) 
    {
        DXCOMMON("(!) Sound::insertSilence : failed to restore buffer.");
        return dxFailed;
    }

    // Lock the buffer down
    if (FAILED((*m_buffer)->Lock(0, m_bufferSize, 
                            &lockedBuffer, (unsigned long*) &lockedBufferSize, 
                            0, 0, 0L)))
    {
        DXCOMMON("(!) Sound::insertSilence : failed to lock buffer.");
        return dxFailed;
    }


    FillMemory(((UByte*)lockedBuffer)+bufferOffset, bufferSize, 0);

    // Unlock the buffer, we don't need it anymore.
    (*m_buffer)->Unlock(lockedBuffer, lockedBufferSize, 0, 0);

    return dxSuccess;
}

/*************************************************************************************
 *@class WaveFile
 *@method
 *    constuctor
 *************************************************************************************/
WaveFile::WaveFile()
{
    m_waveFormat    = NULL;
    m_mmioHandle   = NULL;
    m_buffer = NULL;
    m_size  = 0;
    m_fromMemory = FALSE;
}




/*************************************************************************************
 *@class WaveFile
 *@method
 *    destructor
 *************************************************************************************/
WaveFile::~WaveFile()
{
    close();

    if (!m_fromMemory)
        SAFE_DELETE_ARRAY( m_waveFormat );
    SAFE_DELETE(m_waveFormat);
}




/*************************************************************************************
 *@class WaveFile
 *@method
 *  Int open(Char* filename, WAVEFORMATEX* format, UInt flags)
 *@parameters
 *    - filename : the filename of the wave you want to open
 *    - format   ; a pointer to the format you wish to open the wave in
 *    - flags : enter WAVEFILE_READ for reading, anything else for writing
 *
 *@returns
 *    - dxSuccess : if successful
 *    - dxFailed  : otherwise
 *
 *************************************************************************************/
Int WaveFile::open(Char* filename, WAVEFORMATEX* format, UInt flags)
{
    m_flags = flags;
    m_fromMemory = false;

    if (m_flags == WAVEFILE_READ)
    {
        if (filename == 0)
            return dxFailed;
        SAFE_DELETE_ARRAY(m_waveFormat);

        m_mmioHandle = mmioOpen(filename, 0, MMIO_ALLOCBUF | MMIO_READ);

        if (m_mmioHandle == 0)
        {
            HRSRC   resourceInfo;
            HGLOBAL resourceData;
            UInt    size;
            void*   result;

            // couldn't find file, try finding resource
            resourceInfo = FindResource(0, filename, TEXT("WAVE"));
            if (resourceInfo == 0) 
            {
                if (0 == (resourceInfo = FindResource(0, filename, TEXT("WAV"))))
                {
                    DXCOMMON("(!) WaveFile::open : failed to locate %s.", filename);
                    return dxFailed;
                }
            }

            resourceData = LoadResource(0, resourceInfo);
            if (resourceData == 0)
            {
                DXCOMMON("(!) WaveFile::open : failed to load %s.", filename);
                return dxFailed;
            }

            size = SizeofResource(0, resourceInfo);
            if (size == 0) 
            {
                DXCOMMON("(!) WaveFile::open : failed to retrieve size of %s.", filename);
                return dxFailed;
            }

            result = LockResource(resourceData);
            if (result == 0)
            {
                DXCOMMON("(!) WaveFile::open : failed to lock resource %s.", filename);
                return dxFailed;
            }

            m_buffer = new Char[size];
            memcpy(m_buffer, result, size);

            MMIOINFO mmioInfo;
            ZeroMemory(&mmioInfo, sizeof(mmioInfo));
            mmioInfo.fccIOProc = FOURCC_MEM;
            mmioInfo.cchBuffer = size;
            mmioInfo.pchBuffer = (Char*) m_buffer;

            m_mmioHandle = mmioOpen(0, &mmioInfo, MMIO_ALLOCBUF | MMIO_READ);
        }

        if (readMmio() != dxSuccess)
        {
            // readMmio will fail if its an not a wave file
            DXCOMMON("(!) WaveFile::open : failed to read MMIO for %s.", filename);
            mmioClose(m_mmioHandle, 0);
            return dxFailed;
        }

        if (resetFile() != dxSuccess)
        {
            DXCOMMON("(!) WaveFile::open : failed to reset file %s.", filename);
            return dxFailed;
        }

        // After the reset, the size of the wav file is m_ckInfo.cksize so store it now
        m_size = m_ckInfo.cksize;
    }
    else
    {
        m_mmioHandle = mmioOpen(filename, 0, MMIO_ALLOCBUF | MMIO_READWRITE | MMIO_CREATE);
        if (0 == m_mmioHandle)
        {
            DXCOMMON("(!) WaveFile::open : failed to open MMIO for %s.", filename);
            return dxFailed;
        }

        if (writeMmio(format) != dxSuccess)
        {
            mmioClose(m_mmioHandle, 0);
            DXCOMMON("(!) WaveFile::open : failed to write MMIO for %s.", filename);
            return dxFailed;
        }
                        
        if (resetFile() != dxSuccess)
        {
            DXCOMMON("(!) WaveFile::open : failed to reset file %s.", filename);
            return dxFailed;
        }
    }

    return dxSuccess;
}



/*************************************************************************************
 *@class WaveFile
 *@method
 *    Int openFromMemory(UByte* buffer, UInt bufferSize, 
 *                       WAVEFORMATEX* format, UInt flags)
 *@parameters
 *    - buffer      : the buffer that contains the WaveFile
 *    - bufferSize  : the size of the buffer (and thus the WaveFile)
 *    - format      : the format of the WaveFile
 *    - flags       : only reading can be done, so must equal WAVEFILE_READ
 *
 *@returns
 *    - dxSuccess   : if successful
 *    - dxFailed    : otherwise
 *
 *************************************************************************************/
Int WaveFile::openFromMemory(UByte* buffer, UInt bufferSize, 
                             WAVEFORMATEX* format, UInt flags)
{
    m_waveFormat    = format;
    m_dataSize      = bufferSize;
    m_data          = buffer;
    m_dataCurrent   = m_data;
    m_fromMemory    = true;
    
    if (flags != WAVEFILE_READ)
        return dxFailed;       
    else
        return dxSuccess;
}




/*************************************************************************************
 *@class WaveFile
 *@method
 *    Int readMmio( )
 *@returns
 *    - dxSuccess : if successful
 *    - dxFailed  : otherwise
 *
 *@remark
 *    m_mmioHandle must be valid before calling this method
 *************************************************************************************/
Int WaveFile::readMmio()
{
    MMCKINFO        ckIn;           // chunk info. for general use.
    PCMWAVEFORMAT   pcmWaveFormat;  // Temp PCM structure to load in.       

    m_waveFormat = NULL;

    if ((0 != mmioDescend(m_mmioHandle, &m_ckInfoRiff, NULL, 0)))
    {
        DXCOMMON("(!) WaveFile::readMmio : failed to descend to MMIO.");
        return dxFailed;
    }

    // Check to make sure this is a valid wave file
    if ((m_ckInfoRiff.ckid != FOURCC_RIFF) ||
        (m_ckInfoRiff.fccType != mmioFOURCC('W', 'A', 'V', 'E')))
    {
        DXCOMMON("(!) WaveFile::readMmio : invalid wavefile.");
        return dxFailed;
    }

    // Search the input file for for the 'fmt ' chunk.
    ckIn.ckid = mmioFOURCC('f', 'm', 't', ' ');
    if (0 != mmioDescend(m_mmioHandle, &ckIn, &m_ckInfoRiff, MMIO_FINDCHUNK))
    {
        DXCOMMON("(!) WaveFile::readMmio : failed to descend to MMIO.");
        return dxFailed;
    }

    // Expect the 'fmt' chunk to be at least as large as <PCMWAVEFORMAT>;
    // if there are extra parameters at the end, we'll ignore them
    if (ckIn.cksize < (LONG) sizeof(PCMWAVEFORMAT))
    {
        DXCOMMON("(!) WaveFile::readMmio : 'fmt' chunk too short.");
        return dxFailed;
    }

    // Read the 'fmt ' chunk into <pcmWaveFormat>.
    if (mmioRead(m_mmioHandle, (HPSTR) &pcmWaveFormat, 
                 sizeof(pcmWaveFormat)) != sizeof(pcmWaveFormat))
    {
        DXCOMMON("(!) WaveFile::readMmio : error reading the actual MMIO.");
        return dxFailed;
    }

    // Allocate the waveformatex, but if its not pcm format, read the next
    // word, and thats how many extra bytes to allocate.
    if (pcmWaveFormat.wf.wFormatTag == WAVE_FORMAT_PCM)
    {
        m_waveFormat = (WAVEFORMATEX*)new Char[sizeof(WAVEFORMATEX)];
        if (m_waveFormat == 0)
        {
            DXCOMMON("(!) WaveFile::readMmio : failed to allocate memory for waveformat.");
            return dxFailed;
        }

        // Copy the bytes from the pcm structure to the waveformatex structure
        memcpy(m_waveFormat, &pcmWaveFormat, sizeof(pcmWaveFormat));
        m_waveFormat->cbSize = 0;
    }
    else
    {
        // Read in length of extra bytes.
        UShort cbExtraBytes = 0L;
        if (mmioRead(m_mmioHandle, (Char*)&cbExtraBytes, sizeof(UShort)) != sizeof(UShort))
        {
            DXCOMMON("(!) WaveFile::readMmio : error reading the actual MMIO.");
            return dxFailed;
        }

        m_waveFormat = (WAVEFORMATEX*)new CHAR[ sizeof(WAVEFORMATEX) + cbExtraBytes ];
        if (m_waveFormat == 0)
        {
            DXCOMMON("(!) WaveFile::readMmio : failed to allocate memory for waveformat.");
            return dxFailed;
        }

        // Copy the bytes from the pcm structure to the waveformatex structure
        memcpy(m_waveFormat, &pcmWaveFormat, sizeof(pcmWaveFormat));
        m_waveFormat->cbSize = cbExtraBytes;

        // Now, read those extra bytes into the structure, if cbExtraAlloc != 0.
        if  (mmioRead(m_mmioHandle, (Char*)(((UByte*)&(m_waveFormat->cbSize))+sizeof(UShort)),
                      cbExtraBytes ) != cbExtraBytes)
        {
            SAFE_DELETE(m_waveFormat);
            DXCOMMON("(!) WaveFile::readMmio : error reading the actual MMIO.");
            return dxFailed;
        }
    }

    // Ascend the input file out of the 'fmt ' chunk.
    if (0 != mmioAscend( m_mmioHandle, &ckIn, 0))
    {
        SAFE_DELETE(m_waveFormat);
        DXCOMMON("(!) WaveFile::readMmio : error descending to the MMIO.");
        return dxFailed;
    }
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::size()
// Desc: Retuns the size of the read access wave file 
//-----------------------------------------------------------------------------
UInt WaveFile::size()
{
    return m_size;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::resetFile()
// Desc: Resets the internal m_ckInfo pointer so reading starts from the 
//       beginning of the file again 
//-----------------------------------------------------------------------------
Int WaveFile::resetFile()
{
    if (m_fromMemory)
    {
        m_dataCurrent = m_data;
    }
    else 
    {
        if (m_mmioHandle == 0)
            return dxFailed;

        if (m_flags == WAVEFILE_READ)
        {
            // Seek to the data
            if (-1 == mmioSeek(m_mmioHandle, m_ckInfoRiff.dwDataOffset + sizeof(FOURCC),
                               SEEK_SET))
            {
                DXCOMMON("(!) WaveFile::resetFile : failed to seek MMIO.");
                return dxFailed;
            }

            // Search the input file for the 'data' chunk.
            m_ckInfo.ckid = mmioFOURCC('d', 'a', 't', 'a');
            if (0 != mmioDescend(m_mmioHandle, &m_ckInfo, &m_ckInfoRiff, MMIO_FINDCHUNK))
            {
                DXCOMMON("(!) WaveFile::resetFile : failed to descend to MMIO.");
                return dxFailed;
            }
        }
        else
        {
            // Create the 'data' chunk that holds the waveform samples.  
            m_ckInfo.ckid = mmioFOURCC('d', 'a', 't', 'a');
            m_ckInfo.cksize = 0;

            if (0 != mmioCreateChunk(m_mmioHandle, &m_ckInfo, 0)) 
            {
                DXCOMMON("(!) WaveFile::resetFile : failed to create data chunk.");
                return dxFailed;
            }

            if (0 != mmioGetInfo(m_mmioHandle, &m_mmioInfo, 0))
            {
                DXCOMMON("(!) WaveFile::resetFile : failed to retrieve MMIO info.");
                return dxFailed;
            }
        }
    }
    
    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::read()
// Desc: reads section of data from a wave file into pBuffer and returns 
//       how much read in pdwSizeRead, reading not more than dwSizeToRead.
//       This uses m_ckInfo to determine where to start reading from.  So 
//       subsequent calls will be continue where the last left off unless 
//       Reset() is called.
//-----------------------------------------------------------------------------
Int WaveFile::read(UByte* pBuffer, UInt dwSizeToRead, UInt* pdwSizeRead)
{
    if (m_fromMemory)
    {
        if (m_dataCurrent == 0)
            return dxFailed;
        if (pdwSizeRead != 0)
            *pdwSizeRead = 0;

        if ((UByte*)(m_dataCurrent + dwSizeToRead) > (UByte*)(m_data + m_dataSize))
        {
            dwSizeToRead = m_dataSize - (UInt)(m_dataCurrent - m_data);
        }
        
        CopyMemory(pBuffer, m_dataCurrent, dwSizeToRead);
        
        if (pdwSizeRead != 0)
            *pdwSizeRead = dwSizeToRead;

        return dxSuccess;
    }
    else 
    {
        MMIOINFO mmioinfoIn; // current status of m_mmioHandle

        if (m_mmioHandle == 0)
            return dxFailed;
        if (pBuffer == 0 || pdwSizeRead == 0)
            return dxFailed;

        if (pdwSizeRead != 0)
            *pdwSizeRead = 0;

        if (0 != mmioGetInfo(m_mmioHandle, &mmioinfoIn, 0))
        {
            DXCOMMON("(!)WaveFile::Read : failed to retrieve MMIO info.");
            return dxFailed;
        }

        UInt cbDataIn = dwSizeToRead;
        if (cbDataIn > m_ckInfo.cksize) 
            cbDataIn = m_ckInfo.cksize;       

        m_ckInfo.cksize -= cbDataIn;
    
        for (UInt cT = 0; cT < cbDataIn; cT++)
        {
            // Copy the bytes from the io to the buffer.
            if (mmioinfoIn.pchNext == mmioinfoIn.pchEndRead)
            {
                if (0 != mmioAdvance(m_mmioHandle, &mmioinfoIn, MMIO_READ))
                {
                    DXCOMMON("(!) WaveFile::read : failed to advance MMIO.");
                    return dxFailed;
                }

                if (mmioinfoIn.pchNext == mmioinfoIn.pchEndRead)
                    return dxFailed;
            }

            // Actual copy.
            *((UByte*)pBuffer+cT) = *((UByte*)mmioinfoIn.pchNext);
            mmioinfoIn.pchNext++;
        }

        if (0 != mmioSetInfo( m_mmioHandle, &mmioinfoIn, 0))
        {
            DXCOMMON("(!)WaveFile::read : error setting MMIO info");
            return dxFailed;
        }

        if (pdwSizeRead != 0)
            *pdwSizeRead = cbDataIn;

        return dxSuccess;
    }
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::close()
// Desc: closes the wave file 
//-----------------------------------------------------------------------------
Int WaveFile::close()
{
    if (m_flags == WAVEFILE_READ)
    {
        mmioClose(m_mmioHandle, 0);
        m_mmioHandle = 0;
        SAFE_DELETE_ARRAY(m_buffer);
    }
    else
    {
        m_mmioInfo.dwFlags |= MMIO_DIRTY;

        if (m_mmioHandle == NULL)
            return dxFailed;

        if (0 != mmioSetInfo(m_mmioHandle, &m_mmioInfo, 0))
        {
            DXCOMMON("(!) WaveFile::close : error setting MMIO info.");
            return dxFailed;
        }
    
        // Ascend the output file out of the 'data' chunk -- this will cause
        // the chunk size of the 'data' chunk to be written.
        if (0 != mmioAscend(m_mmioHandle, &m_ckInfo, 0))
        {
            DXCOMMON("(!) WaveFile::close : error ascending to the data chunk.");
            return dxFailed;
        }
    
        // Do this here instead...
        if (0 != mmioAscend(m_mmioHandle, &m_ckInfoRiff, 0))
        {
            DXCOMMON("(!) WaveFile::close : error ascending to the data chunk.");
            return dxFailed;
        }

        mmioSeek(m_mmioHandle, 0, SEEK_SET);

        if (0 != (Int)mmioDescend(m_mmioHandle, &m_ckInfoRiff, 0, 0))
        {
            DXCOMMON("(!) WaveFile::close : error descending to the data chunk.");
            return dxFailed;
        }

        m_ckInfo.ckid = mmioFOURCC('f', 'a', 'c', 't');

        if (0 == mmioDescend( m_mmioHandle, &m_ckInfo, &m_ckInfoRiff, MMIO_FINDCHUNK ) ) 
        {
            UInt dwSamples = 0;
            mmioWrite(m_mmioHandle, (HPSTR)&dwSamples, sizeof(UInt));
            mmioAscend(m_mmioHandle, &m_ckInfo, 0); 
        }
    
        // Ascend the output file out of the 'RIFF' chunk -- this will cause
        // the chunk size of the 'RIFF' chunk to be written.
        if (0 != mmioAscend(m_mmioHandle, &m_ckInfoRiff, 0))
        {
            DXCOMMON("(!) WaveFile::close : error ascending out of RIFF chunk.");
            return dxFailed;
        }

        mmioClose( m_mmioHandle, 0 );
        m_mmioHandle = 0;
    }

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::writeMmio()
// Desc: Support function for reading from a multimedia I/O stream
//       pwfxDest is the WAVEFORMATEX for this new wave file.  
//       m_mmioHandle must be valid before calling.  This function uses it to
//       update m_ckInfoRiff, and m_ckInfo.  
//-----------------------------------------------------------------------------
Int WaveFile::writeMmio(WAVEFORMATEX *pwfxDest)
{
    UInt    dwFactChunk; // Contains the actual fact chunk. Garbage until WaveCloseWriteFile.
    MMCKINFO ckOut1;
    
    dwFactChunk = (DWORD)-1;

    // Create the output file RIFF chunk of form type 'WAVE'.
    m_ckInfoRiff.fccType = mmioFOURCC('W', 'A', 'V', 'E');       
    m_ckInfoRiff.cksize = 0;

    if (0 != mmioCreateChunk(m_mmioHandle, &m_ckInfoRiff, MMIO_CREATERIFF))
    {
        DXCOMMON("(!) WaveFile::writeMmio : failed to create RIFF chunk.");
        return dxFailed;
    }

    // We are now descended into the 'RIFF' chunk we just created.
    // Now create the 'fmt ' chunk. Since we know the size of this chunk,
    // specify it in the MMCKINFO structure so MMIO doesn't have to seek
    // back and set the chunk size after ascending from the chunk.
    m_ckInfo.ckid = mmioFOURCC('f', 'm', 't', ' ');
    m_ckInfo.cksize = sizeof(PCMWAVEFORMAT);   

    if (0 != mmioCreateChunk( m_mmioHandle, &m_ckInfo, 0))
    {
        DXCOMMON("(!) WaveFile::writeMmio : failed to create the fmt chunk.");
        return dxFailed;
    }

    // Write the PCMWAVEFORMAT structure to the 'fmt ' chunk if its that type. 
    if (pwfxDest->wFormatTag == WAVE_FORMAT_PCM)
    {
        if (mmioWrite(m_mmioHandle, (HPSTR) pwfxDest, 
                      sizeof(PCMWAVEFORMAT)) != sizeof(PCMWAVEFORMAT))
        {
            DXCOMMON("(!) WaveFile::writeMmio : failed to write the fmt chunk.");
            return dxFailed;
        }
    }   
    else 
    {
        // Write the variable length size.
        if ((UInt)mmioWrite( m_mmioHandle, (HPSTR) pwfxDest, 
                             sizeof(*pwfxDest) + pwfxDest->cbSize ) != 
                             ( sizeof(*pwfxDest) + pwfxDest->cbSize))
        {
            DXCOMMON("(!) WaveFile::writeMmio : failed to write the fmt chunk.");
            return dxFailed;
        }
    }  
    
    // Ascend out of the 'fmt ' chunk, back into the 'RIFF' chunk.
    if (0 != mmioAscend(m_mmioHandle, &m_ckInfo, 0))
    {
        DXCOMMON("(!) WaveFile::writeMmio : failed to ascend out of fmt chunk.");
        return dxFailed;
    }

    // Now create the fact chunk, not required for PCM but nice to have.  This is filled
    // in when the close routine is called.
    ckOut1.ckid = mmioFOURCC('f', 'a', 'c', 't');
    ckOut1.cksize = 0;

    if (0 != mmioCreateChunk( m_mmioHandle, &ckOut1, 0))
    {
        DXCOMMON("(!) WaveFile::writeMmio : failed to create fact chunk.");
        return dxFailed;
    }

    if (mmioWrite(m_mmioHandle, (HPSTR)&dwFactChunk, sizeof(dwFactChunk)) != sizeof(dwFactChunk))
    {
        DXCOMMON("(!) WaveFile::writeMmio : failed to write fact chunk.");
        return dxFailed;
    }

    // Now ascend out of the fact chunk...
    if (0 != mmioAscend(m_mmioHandle, &ckOut1, 0))
    {
        DXCOMMON("(!) WaveFile::writeMmio : failed to ascend out of fact chunk.");
        return dxFailed;
    }  

    return dxSuccess;
}




//-----------------------------------------------------------------------------
// Name: CWaveFile::write()
// Desc: writes data to the open wave file
//-----------------------------------------------------------------------------
Int WaveFile::write(UInt nSizeToWrite, UByte* pbSrcData, UInt* pnSizeWrote)
{
    UInt cT;

    if (m_fromMemory)
        return dxFailed;
    if (m_mmioHandle == 0)
        return dxFailed;
    if (pnSizeWrote == 0 || pbSrcData == 0)
        return dxFailed;

    *pnSizeWrote = 0;
    
    for (cT = 0; cT < nSizeToWrite; cT++)
    {       
        if (m_mmioInfo.pchNext == m_mmioInfo.pchEndWrite)
        {
            m_mmioInfo.dwFlags |= MMIO_DIRTY;
            if (0 != mmioAdvance( m_mmioHandle, &m_mmioInfo, MMIO_WRITE))
            {
                DXCOMMON("WafeFile::Write : failed to advance.");
                return dxFailed;
            }
        }

        *((UByte*)m_mmioInfo.pchNext) = *((UByte*)pbSrcData+cT);
        (UByte*)m_mmioInfo.pchNext++;

        (*pnSizeWrote)++;
    }

    return dxSuccess;
}


Listener3D::Listener3D(SoundManager* soundManager) :
    m_ds3DListener(0)
{
    DXCOMMON("(+) Listener3D");
    if (soundManager->listener3DInterface(&m_ds3DListener) != dxSuccess)
    {
        DXCOMMON("(!) Listener3D : failed to get listener3DInterface from SoundManager");
    }

    m_parameters.dwSize = sizeof(DS3DLISTENER);
    if (m_ds3DListener)
        m_ds3DListener->GetAllParameters(&m_parameters);
}


Listener3D::~Listener3D( )
{
    DXCOMMON("(-) Listener3D");
    SAFE_RELEASE(m_ds3DListener);

}



void
Listener3D::parameters(Float dopplerFactor, Float rolloffFactor)
{
    // Every change to 3-D sound buffer and listener settings causes 
    // DirectSound to remix, at the expense of CPU cycles. 
    // To minimize the performance impact of changing 3-D settings, 
    // use the DS3D_DEFERRED flag in the dwApply parameter of any of 
    // the IDirectSound3DListener or IDirectSound3DBuffer methods that 
    // change 3-D settings. Then call the IDirectSound3DListener::CommitDeferredSettings 
    // method to execute all of the deferred commands at once.
    DWORD applyFlag = DS3D_IMMEDIATE;

    m_parameters.flDopplerFactor = dopplerFactor;
    m_parameters.flRolloffFactor = rolloffFactor;
    
    if (m_ds3DListener)
        m_ds3DListener->SetAllParameters(&m_parameters, applyFlag);

}

void
Listener3D::position(Vector3 pos)
{
    DWORD applyFlag = DS3D_IMMEDIATE;
    D3DVECTOR dxVector = {pos.x, pos.y, pos.z};
    m_parameters.vPosition = dxVector;

    if (m_ds3DListener)
        m_ds3DListener->SetPosition(pos.x, pos.y, pos.z, applyFlag);
}


void
Listener3D::orientation(Float dir)
{
    DWORD applyFlag = DS3D_IMMEDIATE;
    D3DVECTOR dxVector = {sinf(dir), 0.0f, cosf(dir)};
    m_parameters.vOrientFront = dxVector;

    if (m_ds3DListener)
        m_ds3DListener->SetOrientation(sinf(dir), 0.0f, cosf(dir), 0.0f, 1.0f, 0.0f, applyFlag);
}


void
Listener3D::positionAndOrientation(Vector3 pos, Float dir)
{
    DWORD applyFlag = DS3D_DEFERRED;

    D3DVECTOR dxVector = {pos.x, pos.y, pos.z};
    m_parameters.vPosition = dxVector;
    D3DVECTOR dxVector2 = {sinf(dir), 0.0f, cosf(dir)};
    m_parameters.vOrientFront = dxVector2;

    if (m_ds3DListener)
    {
        m_ds3DListener->SetPosition(pos.x, pos.y, pos.z, applyFlag);
        m_ds3DListener->SetOrientation(sinf(dir), 0.0f, cosf(dir), 0.0f, 1.0f, 0.0f, applyFlag);
        m_ds3DListener->CommitDeferredSettings( );
    }

}

}; // namespace DX
