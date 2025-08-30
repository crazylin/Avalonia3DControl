# Avalonia3DControl

A cross-platform 3D control for Avalonia applications with OpenGL rendering support.

## Features

- **Cross-platform**: Built on Avalonia framework, supports Windows, macOS, and Linux
- **OpenGL Rendering**: High-performance 3D rendering using OpenGL
- **Multiple Projection Modes**: Switch between perspective and orthographic projections
- **Coordinate Axes**: Visual coordinate system with independent shader rendering
- **Multiple Shading Modes**: Support for vertex shading and texture shading
- **Interactive Camera**: Mouse-controlled camera rotation and zoom
- **Mini Axes**: Corner mini-axes for orientation reference
- **Modern UI**: Clean and intuitive user interface

## Screenshots

*Screenshots will be added here*

## Requirements

- .NET 8.0 or later
- OpenGL 3.3 or later
- Avalonia 11.0 or later

## Installation

1. Clone the repository:
```bash
git clone https://github.com/crazylin/Avalonia3DControl.git
cd Avalonia3DControl
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

4. Run the application:
```bash
dotnet run
```

## Usage

### Basic Usage

The `OpenGL3DControl` can be integrated into any Avalonia application:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:Avalonia3DControl">
    <local:OpenGL3DControl />
</UserControl>
```

### Camera Controls

- **Mouse Drag**: Rotate the camera around the scene
- **Mouse Wheel**: Zoom in/out
- **Projection Toggle**: Switch between perspective and orthographic views

### Rendering Modes

- **Vertex Shading**: Display models with vertex colors
- **Texture Shading**: Display models with texture mapping

## Architecture

### Core Components

- **OpenGL3DControl**: Main 3D control component
- **OpenGLRenderer**: OpenGL rendering engine
- **Scene3D**: 3D scene management
- **Camera**: Camera system with projection controls
- **Model3D**: 3D model representation
- **Material**: Material and shading system

### Project Structure

```
Avalonia3DControl/
├── Core/
│   ├── Cameras/          # Camera system
│   ├── Lighting/         # Lighting system
│   ├── Models/           # 3D model classes
│   └── Scene3D.cs        # Scene management
├── Geometry/
│   └── Factories/        # Geometry generation
├── Materials/            # Material and shading
├── Rendering/
│   └── OpenGL/           # OpenGL rendering engine
├── UI/                   # User interface
└── OpenGL3DControl.cs    # Main control
```

## Technical Details

### OpenGL Features

- Vertex Buffer Objects (VBO)
- Element Buffer Objects (EBO)
- Vertex Array Objects (VAO)
- Shader programs (vertex and fragment shaders)
- Depth testing
- Face culling

### Coordinate System

- Right-handed coordinate system
- Y-axis pointing up
- Z-axis pointing towards the viewer
- Independent coordinate axes rendering

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Avalonia](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [OpenGL](https://www.opengl.org/) - Graphics API
- [OpenTK](https://opentk.net/) - OpenGL bindings for .NET

## Contact

- Author: crazylin
- Email: crazylin@msn.com
- GitHub: [crazylin](https://github.com/crazylin)

---

*For Chinese documentation, see [README_CN.md](README_CN.md)*