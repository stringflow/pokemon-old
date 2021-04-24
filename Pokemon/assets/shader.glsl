#if defined(VS_BUILD)

attribute vec4 Position;
attribute vec4 Color;
attribute vec2 TexCoord;
attribute float TexIndex;

varying vec4 Color0;
varying vec2 TexCoord0;
varying float TexIndex0;

void main() {
    Color0 = Color;
    TexCoord0 = TexCoord;
    TexIndex0 = TexIndex;
    gl_Position = Position;
}

#elif defined(FS_BUILD)

varying vec4 Color0;
varying vec2 TexCoord0;
varying float TexIndex0;

uniform sampler2D Textures[32];

void main() {
    gl_FragColor = texture2D(Textures[int(TexIndex0)], TexCoord0) * Color0;
}

#endif