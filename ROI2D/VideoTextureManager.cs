using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL4.PixelType;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 视频帧信息
    /// </summary>
    public class VideoFrame
    {
        /// <summary>
        /// 帧数据
        /// </summary>
        public byte[] Data { get; set; }
        
        /// <summary>
        /// 帧宽度
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// 帧高度
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// 像素格式
        /// </summary>
        public VideoPixelFormat PixelFormat { get; set; }
        
        /// <summary>
        /// 帧时间戳（毫秒）
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// 帧索引
        /// </summary>
        public int FrameIndex { get; set; }
        
        /// <summary>
        /// 是否为关键帧
        /// </summary>
        public bool IsKeyFrame { get; set; }
        
        /// <summary>
        /// 帧大小（字节）
        /// </summary>
        public int Size => Data?.Length ?? 0;
    }

    /// <summary>
    /// 视频像素格式
    /// </summary>
    public enum VideoPixelFormat
    {
        RGB24,      // RGB 24位
        RGBA32,     // RGBA 32位
        BGR24,      // BGR 24位
        BGRA32,     // BGRA 32位
        YUV420P,    // YUV 4:2:0 平面
        NV12,       // NV12格式
        GRAY8       // 灰度 8位
    }

    /// <summary>
    /// 视频播放状态
    /// </summary>
    public enum VideoPlayState
    {
        Stopped,    // 停止
        Playing,    // 播放
        Paused,     // 暂停
        Loading,    // 加载中
        Error       // 错误
    }

    /// <summary>
    /// 视频信息
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// 视频文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 视频宽度
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// 视频高度
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// 帧率
        /// </summary>
        public double FrameRate { get; set; }
        
        /// <summary>
        /// 总帧数
        /// </summary>
        public int TotalFrames { get; set; }
        
        /// <summary>
        /// 视频时长（毫秒）
        /// </summary>
        public long Duration { get; set; }
        
        /// <summary>
        /// 像素格式
        /// </summary>
        public VideoPixelFormat PixelFormat { get; set; }
        
        /// <summary>
        /// 编码格式
        /// </summary>
        public string Codec { get; set; } = string.Empty;
        
        /// <summary>
        /// 比特率
        /// </summary>
        public long Bitrate { get; set; }
        
        /// <summary>
        /// 是否有音频
        /// </summary>
        public bool HasAudio { get; set; }
    }

    /// <summary>
    /// 纹理缓存项
    /// </summary>
    public class TextureCacheItem
    {
        /// <summary>
        /// OpenGL纹理ID
        /// </summary>
        public int TextureId { get; set; }
        
        /// <summary>
        /// 帧索引
        /// </summary>
        public int FrameIndex { get; set; }
        
        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 纹理宽度
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// 纹理高度
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// 内存使用量（字节）
        /// </summary>
        public int MemoryUsage => Width * Height * 4; // RGBA
    }

    /// <summary>
    /// 视频纹理管理器配置
    /// </summary>
    public class VideoTextureConfig
    {
        /// <summary>
        /// 最大缓存帧数
        /// </summary>
        public int MaxCacheFrames { get; set; } = 30;
        
        /// <summary>
        /// 最大内存使用量（MB）
        /// </summary>
        public int MaxMemoryUsageMB { get; set; } = 100;
        
        /// <summary>
        /// 预加载帧数
        /// </summary>
        public int PreloadFrames { get; set; } = 5;
        
        /// <summary>
        /// 纹理过滤模式
        /// </summary>
        public TextureMinFilter MinFilter { get; set; } = TextureMinFilter.Linear;
        
        /// <summary>
        /// 纹理放大过滤模式
        /// </summary>
        public TextureMagFilter MagFilter { get; set; } = TextureMagFilter.Linear;
        
        /// <summary>
        /// 纹理包装模式
        /// </summary>
        public TextureWrapMode WrapMode { get; set; } = TextureWrapMode.ClampToEdge;
        
        /// <summary>
        /// 是否启用异步加载
        /// </summary>
        public bool EnableAsyncLoading { get; set; } = true;
        
        /// <summary>
        /// 缓存清理间隔（毫秒）
        /// </summary>
        public int CacheCleanupIntervalMs { get; set; } = 5000;
    }

    /// <summary>
    /// 视频纹理管理器
    /// </summary>
    public class VideoTextureManager : IDisposable
    {
        private readonly Dictionary<int, TextureCacheItem> _textureCache = new Dictionary<int, TextureCacheItem>();
        private readonly VideoTextureConfig _config;
        private readonly Timer _cleanupTimer;
        private readonly object _cacheLock = new object();
        
        private VideoInfo _currentVideo;
        private VideoPlayState _playState = VideoPlayState.Stopped;
        private int _currentFrameIndex = 0;
        private long _playStartTime;
        private bool _disposed = false;
        
        /// <summary>
        /// 当前视频信息
        /// </summary>
        public VideoInfo CurrentVideo => _currentVideo;
        
        /// <summary>
        /// 播放状态
        /// </summary>
        public VideoPlayState PlayState => _playState;
        
        /// <summary>
        /// 当前帧索引
        /// </summary>
        public int CurrentFrameIndex => _currentFrameIndex;
        
        /// <summary>
        /// 缓存的纹理数量
        /// </summary>
        public int CachedTextureCount
        {
            get
            {
                lock (_cacheLock)
                {
                    return _textureCache.Count;
                }
            }
        }
        
        /// <summary>
        /// 当前内存使用量（MB）
        /// </summary>
        public double MemoryUsageMB
        {
            get
            {
                lock (_cacheLock)
                {
                    return _textureCache.Values.Sum(item => item.MemoryUsage) / (1024.0 * 1024.0);
                }
            }
        }
        
        /// <summary>
        /// 视频加载事件
        /// </summary>
        public event EventHandler<VideoLoadedEventArgs> VideoLoaded;
        
        /// <summary>
        /// 帧更新事件
        /// </summary>
        public event EventHandler<FrameUpdatedEventArgs> FrameUpdated;
        
        /// <summary>
        /// 播放状态变更事件
        /// </summary>
        public event EventHandler<PlayStateChangedEventArgs> PlayStateChanged;
        
        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<VideoErrorEventArgs> Error;
        
        public VideoTextureManager(VideoTextureConfig config = null)
        {
            _config = config ?? new VideoTextureConfig();
            
            // 启动缓存清理定时器
            _cleanupTimer = new Timer(CleanupCache, null, 
                _config.CacheCleanupIntervalMs, _config.CacheCleanupIntervalMs);
        }
        
        /// <summary>
        /// 加载视频文件
        /// </summary>
        public async Task<bool> LoadVideoAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnError(new VideoErrorEventArgs { Message = $"Video file not found: {filePath}" });
                    return false;
                }
                
                SetPlayState(VideoPlayState.Loading);
                
                // 这里应该使用实际的视频解码库（如FFMpeg.NET）
                // 为了演示，我们创建一个模拟的视频信息
                var videoInfo = await LoadVideoInfoAsync(filePath);
                if (videoInfo == null)
                {
                    OnError(new VideoErrorEventArgs { Message = "Failed to load video information" });
                    return false;
                }
                
                // 清理旧的缓存
                ClearCache();
                
                _currentVideo = videoInfo;
                _currentFrameIndex = 0;
                
                SetPlayState(VideoPlayState.Stopped);
                
                OnVideoLoaded(new VideoLoadedEventArgs { VideoInfo = videoInfo });
                
                return true;
            }
            catch (Exception ex)
            {
                OnError(new VideoErrorEventArgs { Message = ex.Message, Exception = ex });
                return false;
            }
        }
        
        /// <summary>
        /// 播放视频
        /// </summary>
        public void Play()
        {
            if (_currentVideo == null || _playState == VideoPlayState.Playing)
                return;
            
            _playStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SetPlayState(VideoPlayState.Playing);
            
            if (_config.EnableAsyncLoading)
            {
                Task.Run(PlaybackLoop);
            }
        }
        
        /// <summary>
        /// 暂停视频
        /// </summary>
        public void Pause()
        {
            if (_playState == VideoPlayState.Playing)
            {
                SetPlayState(VideoPlayState.Paused);
            }
        }
        
        /// <summary>
        /// 停止视频
        /// </summary>
        public void Stop()
        {
            SetPlayState(VideoPlayState.Stopped);
            _currentFrameIndex = 0;
        }
        
        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        public void SeekToFrame(int frameIndex)
        {
            if (_currentVideo == null)
                return;
            
            frameIndex = Math.Max(0, Math.Min(frameIndex, _currentVideo.TotalFrames - 1));
            _currentFrameIndex = frameIndex;
            
            // 重新计算播放开始时间
            if (_playState == VideoPlayState.Playing)
            {
                var frameTime = (long)(frameIndex * 1000.0 / _currentVideo.FrameRate);
                _playStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - frameTime;
            }
        }
        
        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        public void SeekToTime(long timeMs)
        {
            if (_currentVideo == null)
                return;
            
            var frameIndex = (int)(timeMs * _currentVideo.FrameRate / 1000.0);
            SeekToFrame(frameIndex);
        }
        
        /// <summary>
        /// 获取当前帧的纹理ID
        /// </summary>
        public int GetCurrentFrameTexture()
        {
            return GetFrameTexture(_currentFrameIndex);
        }
        
        /// <summary>
        /// 获取指定帧的纹理ID
        /// </summary>
        public int GetFrameTexture(int frameIndex)
        {
            if (_currentVideo == null || frameIndex < 0 || frameIndex >= _currentVideo.TotalFrames)
                return 0;
            
            lock (_cacheLock)
            {
                if (_textureCache.TryGetValue(frameIndex, out var cacheItem))
                {
                    cacheItem.LastAccessTime = DateTime.Now;
                    return cacheItem.TextureId;
                }
            }
            
            // 异步加载帧
            if (_config.EnableAsyncLoading)
            {
                Task.Run(() => LoadFrameAsync(frameIndex));
            }
            else
            {
                LoadFrameAsync(frameIndex).Wait();
                
                lock (_cacheLock)
                {
                    if (_textureCache.TryGetValue(frameIndex, out var cacheItem))
                    {
                        return cacheItem.TextureId;
                    }
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// 预加载帧
        /// </summary>
        public void PreloadFrames(int startFrame, int count)
        {
            if (_currentVideo == null)
                return;
            
            Task.Run(async () =>
            {
                for (int i = 0; i < count; i++)
                {
                    var frameIndex = startFrame + i;
                    if (frameIndex >= 0 && frameIndex < _currentVideo.TotalFrames)
                    {
                        await LoadFrameAsync(frameIndex);
                    }
                }
            });
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                foreach (var item in _textureCache.Values)
                {
                    GL.DeleteTexture(item.TextureId);
                }
                _textureCache.Clear();
            }
        }
        
        /// <summary>
        /// 获取视频统计信息
        /// </summary>
        public VideoStatistics GetStatistics()
        {
            lock (_cacheLock)
            {
                return new VideoStatistics
                {
                    CurrentFrameIndex = _currentFrameIndex,
                    TotalFrames = _currentVideo?.TotalFrames ?? 0,
                    CachedFrames = _textureCache.Count,
                    MemoryUsageMB = MemoryUsageMB,
                    PlayState = _playState,
                    CurrentTimeMs = _currentVideo != null ? (long)(_currentFrameIndex * 1000.0 / _currentVideo.FrameRate) : 0,
                    TotalTimeMs = _currentVideo?.Duration ?? 0
                };
            }
        }
        
        /// <summary>
        /// 异步加载帧
        /// </summary>
        private async Task LoadFrameAsync(int frameIndex)
        {
            try
            {
                // 检查是否已经缓存
                lock (_cacheLock)
                {
                    if (_textureCache.ContainsKey(frameIndex))
                        return;
                }
                
                // 这里应该使用实际的视频解码库来获取帧数据
                var frame = await DecodeFrameAsync(frameIndex);
                if (frame == null)
                    return;
                
                // 创建OpenGL纹理
                var textureId = CreateTexture(frame);
                if (textureId == 0)
                    return;
                
                // 添加到缓存
                lock (_cacheLock)
                {
                    if (!_textureCache.ContainsKey(frameIndex))
                    {
                        _textureCache[frameIndex] = new TextureCacheItem
                        {
                            TextureId = textureId,
                            FrameIndex = frameIndex,
                            Width = frame.Width,
                            Height = frame.Height
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(new VideoErrorEventArgs { Message = $"Failed to load frame {frameIndex}: {ex.Message}", Exception = ex });
            }
        }
        
        /// <summary>
        /// 播放循环
        /// </summary>
        private async Task PlaybackLoop()
        {
            while (_playState == VideoPlayState.Playing && _currentVideo != null)
            {
                try
                {
                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var elapsedTime = currentTime - _playStartTime;
                    var targetFrame = (int)(elapsedTime * _currentVideo.FrameRate / 1000.0);
                    
                    if (targetFrame >= _currentVideo.TotalFrames)
                    {
                        // 视频播放完成
                        Stop();
                        break;
                    }
                    
                    if (targetFrame != _currentFrameIndex)
                    {
                        _currentFrameIndex = targetFrame;
                        
                        // 预加载后续帧
                        PreloadFrames(_currentFrameIndex + 1, _config.PreloadFrames);
                        
                        OnFrameUpdated(new FrameUpdatedEventArgs
                        {
                            FrameIndex = _currentFrameIndex,
                            Timestamp = elapsedTime,
                            TextureId = GetFrameTexture(_currentFrameIndex)
                        });
                    }
                    
                    // 控制帧率
                    var frameTime = 1000.0 / _currentVideo.FrameRate;
                    await Task.Delay((int)Math.Max(1, frameTime / 2));
                }
                catch (Exception ex)
                {
                    OnError(new VideoErrorEventArgs { Message = ex.Message, Exception = ex });
                    break;
                }
            }
        }
        
        /// <summary>
        /// 缓存清理
        /// </summary>
        private void CleanupCache(object state)
        {
            if (_disposed)
                return;
            
            lock (_cacheLock)
            {
                var now = DateTime.Now;
                var itemsToRemove = new List<int>();
                
                // 按内存使用量清理
                while (MemoryUsageMB > _config.MaxMemoryUsageMB && _textureCache.Count > 0)
                {
                    var oldestItem = _textureCache.Values.OrderBy(item => item.LastAccessTime).First();
                    itemsToRemove.Add(oldestItem.FrameIndex);
                }
                
                // 按数量限制清理
                while (_textureCache.Count > _config.MaxCacheFrames)
                {
                    var oldestItem = _textureCache.Values.OrderBy(item => item.LastAccessTime).First();
                    if (!itemsToRemove.Contains(oldestItem.FrameIndex))
                    {
                        itemsToRemove.Add(oldestItem.FrameIndex);
                    }
                }
                
                // 移除过期项
                foreach (var frameIndex in itemsToRemove)
                {
                    if (_textureCache.TryGetValue(frameIndex, out var item))
                    {
                        GL.DeleteTexture(item.TextureId);
                        _textureCache.Remove(frameIndex);
                    }
                }
            }
        }
        
        /// <summary>
        /// 加载视频信息（模拟实现）
        /// </summary>
        private async Task<VideoInfo> LoadVideoInfoAsync(string filePath)
        {
            // 这里应该使用实际的视频解码库
            // 为了演示，返回模拟数据
            await Task.Delay(100); // 模拟异步操作
            
            return new VideoInfo
            {
                FilePath = filePath,
                Width = 1920,
                Height = 1080,
                FrameRate = 30.0,
                TotalFrames = 3000, // 100秒的视频
                Duration = 100000,   // 100秒
                PixelFormat = VideoPixelFormat.RGBA32,
                Codec = "H.264",
                Bitrate = 5000000,
                HasAudio = true
            };
        }
        
        /// <summary>
        /// 解码帧（模拟实现）
        /// </summary>
        private async Task<VideoFrame> DecodeFrameAsync(int frameIndex)
        {
            // 这里应该使用实际的视频解码库
            // 为了演示，返回模拟数据
            await Task.Delay(10); // 模拟解码时间
            
            if (_currentVideo == null)
                return null;
            
            var width = _currentVideo.Width;
            var height = _currentVideo.Height;
            var data = new byte[width * height * 4]; // RGBA
            
            // 生成测试图案
            var random = new Random(frameIndex);
            for (int i = 0; i < data.Length; i += 4)
            {
                data[i] = (byte)random.Next(256);     // R
                data[i + 1] = (byte)random.Next(256); // G
                data[i + 2] = (byte)random.Next(256); // B
                data[i + 3] = 255;                    // A
            }
            
            return new VideoFrame
            {
                Data = data,
                Width = width,
                Height = height,
                PixelFormat = VideoPixelFormat.RGBA32,
                Timestamp = (long)(frameIndex * 1000.0 / _currentVideo.FrameRate),
                FrameIndex = frameIndex,
                IsKeyFrame = frameIndex % 30 == 0
            };
        }
        
        /// <summary>
        /// 创建OpenGL纹理
        /// </summary>
        private int CreateTexture(VideoFrame frame)
        {
            try
            {
                var textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                
                // 设置纹理参数
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)_config.MinFilter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)_config.MagFilter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)_config.WrapMode);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)_config.WrapMode);
                
                // 上传纹理数据
                var pixelFormat = GetOpenGLPixelFormat(frame.PixelFormat);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                    frame.Width, frame.Height, 0, pixelFormat, PixelType.UnsignedByte, frame.Data);
                
                GL.BindTexture(TextureTarget.Texture2D, 0);
                
                return textureId;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// 获取OpenGL像素格式
        /// </summary>
        private PixelFormat GetOpenGLPixelFormat(VideoPixelFormat format)
        {
            return format switch
            {
                VideoPixelFormat.RGB24 => PixelFormat.Rgb,
                VideoPixelFormat.RGBA32 => PixelFormat.Rgba,
                VideoPixelFormat.BGR24 => PixelFormat.Bgr,
                VideoPixelFormat.BGRA32 => PixelFormat.Bgra,
                VideoPixelFormat.GRAY8 => PixelFormat.Red,
                _ => PixelFormat.Rgba
            };
        }
        
        /// <summary>
        /// 设置播放状态
        /// </summary>
        private void SetPlayState(VideoPlayState newState)
        {
            if (_playState != newState)
            {
                var oldState = _playState;
                _playState = newState;
                
                OnPlayStateChanged(new PlayStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = newState
                });
            }
        }
        
        /// <summary>
        /// 触发视频加载事件
        /// </summary>
        protected virtual void OnVideoLoaded(VideoLoadedEventArgs e)
        {
            VideoLoaded?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发帧更新事件
        /// </summary>
        protected virtual void OnFrameUpdated(FrameUpdatedEventArgs e)
        {
            FrameUpdated?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发播放状态变更事件
        /// </summary>
        protected virtual void OnPlayStateChanged(PlayStateChangedEventArgs e)
        {
            PlayStateChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发错误事件
        /// </summary>
        protected virtual void OnError(VideoErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                _cleanupTimer?.Dispose();
                ClearCache();
                
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    /// 视频统计信息
    /// </summary>
    public class VideoStatistics
    {
        public int CurrentFrameIndex { get; set; }
        public int TotalFrames { get; set; }
        public int CachedFrames { get; set; }
        public double MemoryUsageMB { get; set; }
        public VideoPlayState PlayState { get; set; }
        public long CurrentTimeMs { get; set; }
        public long TotalTimeMs { get; set; }
        public double Progress => TotalFrames > 0 ? (double)CurrentFrameIndex / TotalFrames : 0.0;
    }

    /// <summary>
    /// 视频加载事件参数
    /// </summary>
    public class VideoLoadedEventArgs : EventArgs
    {
        public VideoInfo VideoInfo { get; set; }
    }

    /// <summary>
    /// 帧更新事件参数
    /// </summary>
    public class FrameUpdatedEventArgs : EventArgs
    {
        public int FrameIndex { get; set; }
        public long Timestamp { get; set; }
        public int TextureId { get; set; }
    }

    /// <summary>
    /// 播放状态变更事件参数
    /// </summary>
    public class PlayStateChangedEventArgs : EventArgs
    {
        public VideoPlayState OldState { get; set; }
        public VideoPlayState NewState { get; set; }
    }

    /// <summary>
    /// 视频错误事件参数
    /// </summary>
    public class VideoErrorEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public Exception Exception { get; set; }
    }
}