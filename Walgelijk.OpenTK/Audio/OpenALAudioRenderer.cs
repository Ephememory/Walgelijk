﻿using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Walgelijk.OpenTK
{
    public class OpenALAudioRenderer : AudioRenderer
    {
        internal const int StreamingBufferSize = 1024;

        private ALDevice device;
        private ALContext context;
        private bool canPlayAudio = false;
        private bool canEnumerateDevices = false;
        private readonly List<TemporarySource> temporarySources = new();

        public override float Volume
        {
            get
            {
                AL.GetListener(ALListenerf.Gain, out var gain);
                return gain;
            }

            set => AL.Listener(ALListenerf.Gain, value);
        }
        public override bool Muted { get => Volume <= float.Epsilon; set => Volume = 0; }
        public override Vector3 ListenerPosition
        {
            get
            {
                AL.GetListener(ALListener3f.Position, out float x, out float depth, out float z);
                return new Vector3(x, z, depth);
            }

            set => AL.Listener(ALListener3f.Position, value.X, value.Z, value.Y);
        }

        public OpenALAudioRenderer()
        {
            Resources.RegisterType(typeof(AudioData), d => LoadSound(d));

            canEnumerateDevices = AL.IsExtensionPresent("ALC_ENUMERATION_EXT");

            Initialise();
        }

        private void Initialise(string? deviceName = null)
        {
            canPlayAudio = false;

            device = ALC.OpenDevice(deviceName);

            if (device == ALDevice.Null)
                Logger.Warn(deviceName == null ? "No audio device could be found" : "The requested audio device could not be found", this);

            context = ALC.CreateContext(device, new ALContextAttributes());
            if (context == ALContext.Null)
                Logger.Warn("No audio context could be created", this);

            bool couldSetContext = ALC.MakeContextCurrent(context);

            canPlayAudio = device != ALDevice.Null && context != ALContext.Null && couldSetContext;

            if (!couldSetContext)
                Logger.Warn("The audio context could not be set", this);

            if (!canPlayAudio)
                Logger.Error("Failed to initialise the audio renderer", this);
        }

        private static void UpdateIfRequired(Sound sound, out int source)
        {
            source = AudioObjects.Sources.Load(sound);

            if (!sound.RequiresUpdate)
                return;

            sound.RequiresUpdate = false;
            AL.Source(source, ALSourceb.SourceRelative, !sound.Spatial);
            AL.Source(source, ALSourceb.Looping, sound.Looping);
            AL.Source(source, ALSourcef.RolloffFactor, sound.RolloffFactor);
            AL.Source(source, ALSourcef.Pitch, sound.Pitch);
        }

        public override AudioData LoadSound(string path, bool streaming = false)
        {
            var ext = path.AsSpan()[path.LastIndexOf('.')..];
            AudioFileData data;

            if (streaming)
                throw new NotImplementedException("This audio renderer can not stream yet.");

            try
            {
                if (Utilities.TextEqualsCaseInsensitive(ext, ".wav"))
                    data = WaveFileReader.Read(path);
                else if (Utilities.TextEqualsCaseInsensitive(ext, ".ogg"))
                    data = VorbisFileReader.Read(path);
                else
                    throw new Exception($"This is not a supported audio file. Only Microsoft WAV and Ogg Vorbis can be decoded.");
            }
            catch (Exception e)
            {
                throw new AggregateException($"Failed to load audio file: {path}", e);
            }
            var audio = new AudioData(data.Data, data.SampleRate, data.NumChannels, data.SampleCount);
            return audio;
        }

        public override void Pause(Sound sound)
        {
            if (!canPlayAudio || sound.Data == null)
                return;

            UpdateIfRequired(sound, out int id);
            AL.SourcePause(id);
        }

        public override void Play(Sound sound, float volume = 1)
        {
            if (!canPlayAudio || sound.Data == null)
                return;

            UpdateIfRequired(sound, out int s);
            SetVolume(sound, 1);
            AL.SourcePlay(s);
        }

        public override void Play(Sound sound, Vector2 worldPosition, float volume = 1)
        {
            if (!canPlayAudio || sound.Data == null)
                return;

            UpdateIfRequired(sound, out int s);
            SetVolume(sound, 1);
            if (sound.Spatial)
                AL.Source(s, ALSource3f.Position, worldPosition.X, 0, worldPosition.Y);
            else
                Logger.Warn("Attempt to play a non-spatial sound in space!");
            AL.SourcePlay(s);
        }

        private int CreateTempSource(Sound sound, float volume, Vector2 worldPosition, float pitch)
        {
            var source = SourceCache.CreateSourceFor(sound);
            AL.Source(source, ALSourceb.SourceRelative, !sound.Spatial);
            AL.Source(source, ALSourceb.Looping, false);
            AL.Source(source, ALSourcef.Gain, volume);
            AL.Source(source, ALSourcef.Pitch, pitch);
            if (sound.Spatial)
                AL.Source(source, ALSource3f.Position, worldPosition.X, 0, worldPosition.Y);
            AL.SourcePlay(source);
            temporarySources.Add(new TemporarySource
            {
                CurrentLifetime = 0,
                Duration = (float)sound.Data.Duration.TotalSeconds,
                Sound = sound,
                Source = source
            });
            return source;
        }

        public override void PlayOnce(Sound sound, float volume = 1, float pitch = 1)
        {
            if (!canPlayAudio || sound.Data == null)
                return;

            UpdateIfRequired(sound, out _);
            CreateTempSource(sound, volume, default, pitch);
        }

        public override void PlayOnce(Sound sound, Vector2 worldPosition, float volume = 1, float pitch = 1)
        {
            if (!canPlayAudio || sound.Data == null)
                return;

            UpdateIfRequired(sound, out _);
            if (!sound.Spatial)
                Logger.Warn("Attempt to play a non-spatial sound in space!");
            CreateTempSource(sound, volume, worldPosition, pitch);
        }

        public override void Stop(Sound sound)
        {
            if (!canPlayAudio || sound.Data == null)
                return;

            UpdateIfRequired(sound, out int s);
            AL.SourceStop(s);
        }

        public override void StopAll()
        {
            if (!canPlayAudio)
                return;

            foreach (var sound in AudioObjects.Sources.GetAllLoaded())
                AL.SourceStop(sound);

            foreach (var item in temporarySources)
            {
                AL.SourceStop(item.Source);
                item.CurrentLifetime = float.MaxValue;
            }
        }

        public override void Release()
        {
            canPlayAudio = false;

            if (device != ALDevice.Null)
                ALC.CloseDevice(device);

            if (context != ALContext.Null)
            {
                ALC.MakeContextCurrent(ALContext.Null);
                ALC.DestroyContext(context);
            }

            foreach (var item in AudioObjects.Buffers.GetAllUnloaded())
                DisposeOf(item);

            foreach (var item in AudioObjects.Sources.GetAllUnloaded())
                DisposeOf(item);

            AudioObjects.Buffers.UnloadAll();
            AudioObjects.Sources.UnloadAll();
        }

        public override void Process(Game game)
        {
            if (!canPlayAudio)
                return;

            for (int i = temporarySources.Count - 1; i >= 0; i--)
            {
                var v = temporarySources[i];
                if (v.CurrentLifetime > v.Duration)
                {
                    AL.DeleteSource(v.Source);
                    temporarySources.Remove(v);
                }
            }

            foreach (var item in temporarySources)
            {
                item.CurrentLifetime += game.Time.UpdateDeltaTime;
            }
        }

        public override bool IsPlaying(Sound sound)
        {
            return AL.GetSourceState(AudioObjects.Sources.Load(sound)) == ALSourceState.Playing;
        }

        public override void SetVolume(Sound sound, float volume)
        {
            var s = AudioObjects.Sources.Load(sound);
            AL.Source(s, ALSourcef.Gain, volume);
        }

        public override void DisposeOf(AudioData audioData)
        {
            if (audioData != null)
            {
                audioData.DisposeLocalCopy();
                AudioObjects.Buffers.Unload(audioData);
                Resources.Unload(audioData);
            }
            //TODO dispose of vorbis reader if applicable
            //if (AudioObjects.VorbisReaderCache.Has())
            //AudioObjects.VorbisReaderCache.Unload(audioData);
        }

        public override void DisposeOf(Sound sound)
        {
            if (sound != null)
            {
                AudioObjects.Sources.Unload(sound);
                Resources.Unload(sound);
            }
        }

        public override void SetAudioDevice(string device)
        {
            Release();
            Initialise(device);
        }

        public override string GetCurrentAudioDevice()
        {
            if (device == ALDevice.Null)
                return null;

            return ALC.GetString(device, AlcGetString.AllDevicesSpecifier);
        }

        public override IEnumerable<string> EnumerateAvailableAudioDevices()
        {
            if (!canEnumerateDevices)
            {
                Logger.Warn("ALC_ENUMERATION_EXT is not present");
                yield break;
            }

            foreach (var deviceName in ALC.GetString(AlcGetStringList.AllDevicesSpecifier))
                yield return deviceName;
        }
    }
}
