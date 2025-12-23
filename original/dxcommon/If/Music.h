/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_MUSIC_H__
#define __DXCOMMON_MUSIC_H__

#include <dmusicc.h>
#include <dmusici.h>
#include <dsound.h>

#include <DxCommon/If/Internal.h>

namespace DirectX
{


class MusicManager;
class MusicSegment;





class MusicManager
{
public:
    enum AudioPathType
    {
        dynamic3D           = DMUS_APATH_DYNAMIC_3D,
        dynamicMono         = DMUS_APATH_DYNAMIC_MONO,
        dynamicStereo       = DMUS_APATH_DYNAMIC_STEREO,
        dynamicStereoReverb = DMUS_APATH_SHARED_STEREOPLUSREVERB
    };

public:
    _dxcommon_ MusicManager();
    _dxcommon_ virtual ~MusicManager();

    _dxcommon_ inline IDirectMusicLoader8*      loader()      { return m_loader; }
    _dxcommon_ inline IDirectMusicPerformance8* performance() { return m_performance; }
    _dxcommon_ IDirectMusicAudioPath8*          defaultAudioPath();

    _dxcommon_ Int initialize(Window::Handle window, UInt nPChannels = 128, AudioPathType defaultPathType = dynamicStereo);

    _dxcommon_ Int  setSearchDirectory(const Char* path);
    _dxcommon_ void collectGarbage();

    _dxcommon_ Int  createSegmentFromFile(MusicSegment** segment, Char* filename, 
		                                  Boolean downloadNow = true, Boolean isMidiFile = false);
    
    _dxcommon_ Int  createSegmentFromResource(MusicSegment** segment, TCHAR* resource, TCHAR* resourceType, 
		                                      Boolean downloadNow = true, Boolean isMidiFile = false);
    //_dxcommon_ Int  createScriptFromFile( CMusicScript** ppScript, TCHAR* strFileName );

    _dxcommon_ Int  createChordMapFromFile(IDirectMusicChordMap8** chordMap, Char* filename);
    _dxcommon_ Int  createStyleFromFile(IDirectMusicStyle8** style, Char* filename);
    _dxcommon_ Int  getMotifFromStyle(IDirectMusicSegment8** motif, Char* style, Char* motifName);

public:
    _dxcommon_ void    volume(UInt volume);
    _dxcommon_ void    groove(Int groove);

protected:
    IDirectMusicLoader8*      m_loader;
    IDirectMusicPerformance8* m_performance;
    Boolean                   m_cleanupCOM;
    Int                       m_groove;

};



class MusicSegment
{
protected:
    IDirectMusicSegment8*     m_segment;
    IDirectMusicLoader8*      m_loader;
    IDirectMusicPerformance8* m_performance;
    IDirectMusicAudioPath8*   m_embeddedAudioPath;
    Boolean                   m_downloaded;

public:
    _dxcommon_ MusicSegment(IDirectMusicPerformance8* performance, 
                            IDirectMusicLoader8* loader,
                            IDirectMusicSegment8* segment);
    _dxcommon_ virtual ~MusicSegment();

    _dxcommon_ inline IDirectMusicSegment8* segment() { return m_segment; }
    _dxcommon_ Int      style(IDirectMusicStyle8** style, UInt styleIndex = 0);

    _dxcommon_ Int      repeats(Int times = -1 /* -1 = infinite */);
    _dxcommon_ Int      play(Boolean looped = false, Boolean secondary = false, IDirectMusicAudioPath8* audioPath = 0);
    _dxcommon_ Int      stop(Boolean immediately = false);
    _dxcommon_ Int      download(IDirectMusicAudioPath8* audioPath = 0);
    _dxcommon_ Int      unload(IDirectMusicAudioPath8* audioPath = 0);

    _dxcommon_ Boolean  isPlaying();
};



/*
//-----------------------------------------------------------------------------
// Name: class CMusicScript
// Desc: Encapsulates functionality of an IDirectMusicScript
//-----------------------------------------------------------------------------
class CMusicScript
{
protected:
    IDirectMusicScript8*      m_pScript;
    IDirectMusicLoader8*      m_pLoader;
    IDirectMusicPerformance8* m_pPerformance;

public:
    CMusicScript( IDirectMusicPerformance8* pPerformance, 
                  IDirectMusicLoader8* pLoader,
                  IDirectMusicScript8* pScript );
    virtual ~CMusicScript();

    inline  IDirectMusicScript8* GetScript() { return m_pScript; }

    HRESULT CallRoutine( TCHAR* strRoutine );
    HRESULT SetVariableNumber( TCHAR* strVariable, LONG lValue );
    HRESULT GetVariableNumber( TCHAR* strVariable, LONG* plValue );
};
*/


} // namespace DirectX


#endif /* __DXCOMMON_MUSIC_H__ */
