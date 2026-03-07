using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DoomNetFrameworkEngine.Audio;
using DoomNetFrameworkEngine.DoomEntity.World;
using UnityEngine;

namespace Elin_JustDoomIt
{
    public sealed class UnityDoomSound : ISound, IDisposable
    {
        private const float DoomSfxSampleGain = 2.2f;
        private readonly string _wadPath;
        private readonly Dictionary<string, LumpEntry> _lumps = new Dictionary<string, LumpEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Sfx, AudioClip> _clipCache = new Dictionary<Sfx, AudioClip>();
        private readonly GameObject _audioGo;
        private readonly AudioSource _source;
        private int _volume = 12;
        private float _externalVolumeScale = 1f;

        private struct LumpEntry
        {
            public int Offset;
            public int Size;
        }

        public int MaxVolume => 15;

        public int Volume
        {
            get => _volume;
            set => _volume = Mathf.Clamp(value, 0, MaxVolume);
        }

        public float ExternalVolumeScale
        {
            get => _externalVolumeScale;
            set
            {
                _externalVolumeScale = Mathf.Clamp(value, 0f, 2f);
                if (_source != null)
                {
                    // Use AudioSource.volume for immediate ducking effect.
                    _source.volume = _externalVolumeScale;
                }
            }
        }

        public UnityDoomSound(string wadPath, int initialVolume)
        {
            _wadPath = wadPath;
            Volume = initialVolume;
            BuildLumpIndex();

            _audioGo = new GameObject("JustDoomIt_Audio");
            UnityEngine.Object.DontDestroyOnLoad(_audioGo);
            _source = _audioGo.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            _source.volume = _externalVolumeScale;
        }

        public void SetListener(Mobj listener)
        {
        }

        public void Update()
        {
        }

        public void StartSound(Sfx sfx)
        {
            PlaySfx(sfx, _volume);
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type)
        {
            PlaySfx(sfx, _volume);
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume)
        {
            PlaySfx(sfx, Mathf.Clamp(volume, 0, MaxVolume));
        }

        public void StopSound(Mobj mobj)
        {
        }

        public void Reset()
        {
            if (_source != null)
            {
                _source.Stop();
            }
        }

        public void Pause()
        {
            if (_source != null)
            {
                _source.Pause();
            }
        }

        public void Resume()
        {
            if (_source != null)
            {
                _source.UnPause();
            }
        }

        public void Dispose()
        {
            foreach (var kv in _clipCache)
            {
                if (kv.Value != null)
                {
                    UnityEngine.Object.Destroy(kv.Value);
                }
            }
            _clipCache.Clear();

            if (_audioGo != null)
            {
                UnityEngine.Object.Destroy(_audioGo);
            }
        }

        private void PlaySfx(Sfx sfx, int volume)
        {
            if (_source == null || sfx == Sfx.NONE)
            {
                return;
            }

            var clip = GetOrCreateClip(sfx);
            if (clip == null)
            {
                return;
            }

            var gain = Mathf.Clamp01(volume / (float)MaxVolume);
            gain *= _externalVolumeScale;
            _source.PlayOneShot(clip, gain);
        }

        private AudioClip GetOrCreateClip(Sfx sfx)
        {
            if (_clipCache.TryGetValue(sfx, out var cached))
            {
                return cached;
            }

            var lumpName = "DS" + sfx.ToString();
            if (!_lumps.TryGetValue(lumpName, out var entry))
            {
                _clipCache[sfx] = null;
                return null;
            }

            try
            {
                using (var fs = File.OpenRead(_wadPath))
                using (var br = new BinaryReader(fs))
                {
                    fs.Position = entry.Offset;
                    var format = br.ReadUInt16();
                    if (format != 3)
                    {
                        DoomDiagnostics.Warn("[JustDoomIt] Unsupported DOOM SFX format in lump: " + lumpName);
                        _clipCache[sfx] = null;
                        return null;
                    }

                    var sampleRate = br.ReadUInt16();
                    var sampleCount = br.ReadInt32();
                    if (sampleRate <= 0 || sampleCount <= 0 || sampleCount > entry.Size - 8)
                    {
                        DoomDiagnostics.Warn("[JustDoomIt] Invalid DOOM SFX header in lump: " + lumpName);
                        _clipCache[sfx] = null;
                        return null;
                    }

                    var raw = br.ReadBytes(sampleCount);
                    var samples = new float[sampleCount];
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var s = ((raw[i] - 128f) / 128f) * DoomSfxSampleGain;
                        samples[i] = Mathf.Clamp(s, -1f, 1f);
                    }

                    var clip = AudioClip.Create("DOOM_" + sfx, sampleCount, 1, sampleRate, false);
                    clip.SetData(samples, 0);
                    _clipCache[sfx] = clip;
                    return clip;
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Failed to decode SFX lump: " + lumpName, ex);
                _clipCache[sfx] = null;
                return null;
            }
        }

        private void BuildLumpIndex()
        {
            try
            {
                using (var fs = File.OpenRead(_wadPath))
                using (var br = new BinaryReader(fs))
                {
                    var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (magic != "IWAD" && magic != "PWAD")
                    {
                        DoomDiagnostics.Warn("[JustDoomIt] Not a WAD file: " + _wadPath);
                        return;
                    }

                    var lumpCount = br.ReadInt32();
                    var directoryOffset = br.ReadInt32();
                    fs.Position = directoryOffset;

                    for (var i = 0; i < lumpCount; i++)
                    {
                        var offset = br.ReadInt32();
                        var size = br.ReadInt32();
                        var name = Encoding.ASCII.GetString(br.ReadBytes(8)).TrimEnd('\0', ' ');
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        _lumps[name] = new LumpEntry
                        {
                            Offset = offset,
                            Size = size
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Failed to index WAD lumps.", ex);
            }
        }
    }
}

