using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL4.PixelType;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 图片背景管理器，用于在调试阶段替代视频功能
    /// </summary>
    public class ImageBackgroundManager : IDisposable
    {
        private int _textureId;
        private int _width;
        private int _height;
        private bool _isLoaded;
        private string _currentImagePath;
        
        /// <summary>
        /// 图片宽度
        /// </summary>
        public int Width => _width;
        
        /// <summary>
        /// 图片高度
        /// </summary>
        public int Height => _height;
        
        /// <summary>
        /// 是否已加载图片
        /// </summary>
        public bool IsLoaded => _isLoaded;
        
        /// <summary>
        /// 当前图片路径
        /// </summary>
        public string CurrentImagePath => _currentImagePath;
        
        /// <summary>
        /// OpenGL纹理ID
        /// </summary>
        public int TextureId => _textureId;
        
        /// <summary>
        /// 图片加载完成事件
        /// </summary>
        public event EventHandler<ImageLoadedEventArgs> ImageLoaded;
        
        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ImageErrorEventArgs> Error;
        
        public ImageBackgroundManager()
        {
            _textureId = 0;
            _width = 0;
            _height = 0;
            _isLoaded = false;
            _currentImagePath = string.Empty;
        }
        
        /// <summary>
        /// 加载图片文件
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <returns>是否加载成功</returns>
        public bool LoadImage(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    OnError(new ImageErrorEventArgs { Message = $"Image file not found: {imagePath}" });
                    return false;
                }
                
                // 清理旧纹理
                ClearTexture();
                
                // 为调试阶段创建一个简单的测试图片
                // 实际项目中可以使用 SkiaSharp 或其他图像库来加载真实图片
                _width = 800;
                _height = 600;
                
                // 创建一个渐变背景图片数据
                var pixelData = new byte[_width * _height * 4]; // RGBA
                
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        var offset = (y * _width + x) * 4;
                        
                        // 创建一个简单的渐变效果
                        var r = (byte)(x * 255 / _width);
                        var g = (byte)(y * 255 / _height);
                        var b = (byte)((x + y) * 255 / (_width + _height));
                        
                        pixelData[offset + 0] = r; // R
                        pixelData[offset + 1] = g; // G
                        pixelData[offset + 2] = b; // B
                        pixelData[offset + 3] = 255; // A (完全不透明)
                    }
                }
                
                // 生成OpenGL纹理
                _textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _textureId);
                
                // 设置纹理参数
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                
                // 上传纹理数据
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    _width,
                    _height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixelData);
                
                GL.BindTexture(TextureTarget.Texture2D, 0);
                
                _currentImagePath = imagePath;
                _isLoaded = true;
                
                OnImageLoaded(new ImageLoadedEventArgs 
                { 
                    ImagePath = imagePath, 
                    Width = _width, 
                    Height = _height 
                });
                
                return true;
            }
            catch (Exception ex)
            {
                OnError(new ImageErrorEventArgs { Message = ex.Message, Exception = ex });
                return false;
            }
        }
        
        /// <summary>
        /// 清理纹理资源
        /// </summary>
        private void ClearTexture()
        {
            if (_textureId != 0)
            {
                GL.DeleteTexture(_textureId);
                _textureId = 0;
            }
            
            _width = 0;
            _height = 0;
            _isLoaded = false;
            _currentImagePath = string.Empty;
        }
        
        /// <summary>
        /// 绑定纹理用于渲染
        /// </summary>
        public void BindTexture()
        {
            if (_isLoaded && _textureId != 0)
            {
                GL.BindTexture(TextureTarget.Texture2D, _textureId);
            }
        }
        
        /// <summary>
        /// 解绑纹理
        /// </summary>
        public void UnbindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        protected virtual void OnImageLoaded(ImageLoadedEventArgs e)
        {
            ImageLoaded?.Invoke(this, e);
        }
        
        protected virtual void OnError(ImageErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            ClearTexture();
        }
    }
    
    /// <summary>
    /// 图片加载完成事件参数
    /// </summary>
    public class ImageLoadedEventArgs : EventArgs
    {
        public string ImagePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    
    /// <summary>
    /// 图片错误事件参数
    /// </summary>
    public class ImageErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}