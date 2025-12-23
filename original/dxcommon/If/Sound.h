/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_SOUND_H__
#define __DXCOMMON_SOUND_H__

#define DIRECTSOUND_VERSION 0x1000

#include <DxCommon/If/Common.h>
#include <mmsystem.h>  
#include <dsound.h>


#ifdef _USE_VORBIS_
#include <vorbis/codec.h>
#include <vorbis/vorbisfile.h>
#endif

namespace DirectX
{

class SoundManager;
class Sound;
class WaveFile;
class Listener3D;

/*************************************************************************************
 *@class SoundManager
 *@description
 *    This class represents the DirectSound interface. It's responsible for 
 *    initiliasing the DirectSound interface and setting the default buffer format.
 *    It has an interface for creating new Sound objects given a Wave file.
 *************************************************************************************/
class SoundManager
{
public:
    enum Algorithm
    {
        AlgoDefault,
        AlgoNoVirtualization,
        AlgoFullHrtf,
        AlgoLightHrtf
    };

public:
    ///@name interface 'constructor/desctructor'
    //@{
    _dxcommon_ SoundManager(::Window::Handle, UInt nChannels, UInt frequency, UInt bitrate);
    _dxcommon_ virtual ~SoundManager();
    //@}

    ///@name interface 'create' method
    //@{
    _dxcommon_ Sound* create(Int resource, Boolean enable3d = false, UInt nBuffers = 1);
    _dxcommon_ Sound* create(Char* filename, Boolean enable3d = false, UInt nBuffers = 1);
    _dxcommon_ Sound* create(DSBUFFERDESC& bufferDesc, Boolean enable3d = false, UInt nBuffers = 1);
#ifdef _USE_VORBIS_
    _dxcommon_ Sound* createVorbis(Char* filename, Boolean enable3d = false, UInt nBuffers = 1);
#endif
    //@}

    ///@name interface 'get/set' methods
    //@{
    _dxcommon_ void           playInSoftware(Boolean val)  { m_playInSoftware = val; }
    _dxcommon_ void           reverseStereo(Boolean val)   { m_reverseStereo = val;  }
    _dxcommon_ LPDIRECTSOUND8 directSound() const          { return m_directSound;   }
    _dxcommon_ Int            bufferFormat(UInt nChannels, UInt frequency, UInt bitrate);
    _dxcommon_ Int            listener3DInterface(LPDIRECTSOUND3DLISTENER* listener);
    _dxcommon_ Algorithm      algorithm( ) const           { return m_3dAlgorithm;   }
    _dxcommon_ void           algorithm(Algorithm algo)    { m_3dAlgorithm = algo;   }
    //@}    

private:
    LPDIRECTSOUND8 m_directSound;
    Boolean        m_created;
    Boolean        m_playInSoftware;
    Boolean        m_reverseStereo;
    Algorithm      m_3dAlgorithm;
};


/*************************************************************************************
 *@class Sound
 *@description
 *    The sound class represents a sound. Besides the normal 'play' and 'stop' 
 *    methods, it has different control methods for controlling volume, pan and
 *    frequency. A sound will usually be created by a 'SoundManager'.
 *************************************************************************************/
class Sound
{
public:
    _dxcommon_ Sound(LPDIRECTSOUNDBUFFER* buffer, UInt bufferSize, UInt nBuffers, WaveFile* waveFile);
    _dxcommon_ Sound(LPDIRECTSOUNDBUFFER* buffer, UInt bufferSize, UInt nBuffers, LPWAVEFORMATEX waveFormat);
#ifdef _USE_VORBIS_
    _dxcommon_ Sound(LPDIRECTSOUNDBUFFER* buffer, UInt bufferSize, UInt nBuffers, 
                     OggVorbis_File* vorbisFile, UShort bitsPerSample, UInt avgBytesPerSec);
#endif
    _dxcommon_ virtual ~Sound();

    _dxcommon_ Int fillBufferWithSound(LPDIRECTSOUNDBUFFER buffer);
    _dxcommon_ Int fillBufferWithSilence(LPDIRECTSOUNDBUFFER buffer, LPWAVEFORMATEX waveFormat);
#ifdef _USE_VORBIS_
    _dxcommon_ Int fillBufferWithSound(LPDIRECTSOUNDBUFFER buffer, OggVorbis_File *vorbisFile, UShort bitsPerSample);
#endif
    _dxcommon_ LPDIRECTSOUNDBUFFER getFreeBuffer();
    
    _dxcommon_ Int play(UInt priority = 0, Boolean looped = FALSE);
    _dxcommon_ Int stop();
    _dxcommon_ Int reset();
    _dxcommon_ Boolean playing();
    _dxcommon_ void playInSoftware(Boolean val)  { m_playInSoftware = val; }
    _dxcommon_ void reverseStereo(Boolean val)   { m_reverseStereo = val ? -1 : 1; }

    ///@name interface 'pan/frequency/volume' methods
    //@{
    _dxcommon_ void pan(Int value);        // value in [-100, +100]
    _dxcommon_ void frequency(Int value);  // value > 0
    _dxcommon_ Int frequency( );
    _dxcommon_ void volume(Int value);     // value in [0, +100]
    _dxcommon_ Int volume( );
    _dxcommon_ Float length( ) { return m_length; }            // value in seconds
    //@}

    ///@name interface '3D sound' methods
    //@{
    _dxcommon_ Int initializeBuffer3D(UInt index = 0);
    _dxcommon_ void position(Vector3 pos);
    //@}

public:
    ///@name interface 'low level access' methods
    //@{
    _dxcommon_ UInt                 bufferSize( ) { return m_bufferSize; }
    _dxcommon_ LPDIRECTSOUNDBUFFER* buffer( )           { return m_buffer;      }
    _dxcommon_ WAVEFORMATEX*        waveFormat( );
    _dxcommon_ UInt                 copyBuffer(LPDIRECTSOUNDBUFFER* buffer, UInt bufferOffset, UInt bufferSize);
    _dxcommon_ UInt                 insertSilence(UInt bufferOffset, UInt bufferSize);
    //@}

protected:
    LPDIRECTSOUNDBUFFER*    m_buffer;
    UInt                    m_bufferSize;
    UInt                    m_nBuffers;
    WaveFile*               m_waveFile;
    Boolean                 m_playInSoftware;
    Int                     m_reverseStereo;
    LPDIRECTSOUND3DBUFFER   m_buffer3D;
    DS3DBUFFER              m_parameters;
    Float                   m_length; // ORDER DEPENDENCY
    
    Int restoreBuffer(LPDIRECTSOUNDBUFFER buffer, Boolean* wasRestored);
};



//-----------------------------------------------------------------------------
// Name: class CWaveFile
// Desc: Encapsulates reading or writing sound data to or from a wave file
//-----------------------------------------------------------------------------
class WaveFile
{
public:
    WAVEFORMATEX* m_waveFormat;        // Pointer to WAVEFORMATEX structure
    HMMIO         m_mmioHandle;       // MM I/O handle for the WAVE
    MMCKINFO      m_ckInfo;          // Multimedia RIFF chunk
    MMCKINFO      m_ckInfoRiff;      // Use in opening a WAVE file
    DWORD         m_size;      // The size of the wave file
    MMIOINFO      m_mmioInfo;
    UInt          m_flags;
    Boolean       m_fromMemory;
    UByte*        m_data;
    UByte*        m_dataCurrent;
    ULong         m_dataSize;
    Byte*         m_buffer;

protected:
    _dxcommon_ Int readMmio();
    _dxcommon_ Int writeMmio( WAVEFORMATEX *pwfxDest );

public:
    _dxcommon_ WaveFile();
    _dxcommon_ virtual ~WaveFile();

    _dxcommon_ Int open(Char* filename, WAVEFORMATEX* format, UInt flags);
    _dxcommon_ Int openFromMemory(UByte* buffer, UInt bufferSize, WAVEFORMATEX* format, UInt flags );
    _dxcommon_ Int close();

    _dxcommon_ Int read( UByte* pBuffer, UInt dwSizeToRead, UInt* pdwSizeRead );
    _dxcommon_ Int write( UInt nSizeToWrite, UByte* pbData, UInt* pnSizeWrote );

    _dxcommon_ UInt size() { return m_size; }
    _dxcommon_ Int resetFile();

    _dxcommon_ WAVEFORMATEX* waveFormat() { return m_waveFormat; };
};


class Listener3D
{
public:
    _dxcommon_ Listener3D(SoundManager* soundManager);
    _dxcommon_ virtual ~Listener3D( );

public:
    _dxcommon_ void parameters(Float dopplerFactor, Float rolloffFactor);
    _dxcommon_ void position(Vector3 pos);
    _dxcommon_ void orientation(Float dir);
    _dxcommon_ void positionAndOrientation(Vector3 pos, Float dir);

protected:
    LPDIRECTSOUND3DLISTENER      m_ds3DListener;
    DS3DLISTENER                 m_parameters;
};


} // namespace DirectX






#endif /* __DXCOMMON_SOUND_H__ */
