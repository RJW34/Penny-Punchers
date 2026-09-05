shader_type canvas_item;
render_mode unshaded;
uniform bool alternate = false;
uniform bool vale = false;
uniform bool chroma_key = false;
void fragment() {
    vec4 c = texture(TEXTURE, UV);
    if(chroma_key) {
        // Background is authored saturated magenta, a color absent from either costume.
        float matte=smoothstep(0.12,0.32,min(c.r,c.b)-c.g);
        c.a*=1.0-matte;
        c.rgb=mix(c.rgb,vec3(c.g),matte*0.65);
    }
    if (alternate) {
        float hi=max(c.r,max(c.g,c.b));
        float lo=min(c.r,min(c.g,c.b));
        if (vale && c.g > c.r * 1.17 && c.b > c.r * 1.08 && hi-lo > 0.07) {
            c.rgb=vec3(c.g*0.84,c.g*0.55,c.g*1.02);
        } else if (!vale && c.b > c.r * 1.30 && c.b > c.g * 1.02 && hi-lo > 0.035) {
            // Navy cloth is separate from the warm skin and amber jacket ramps.
            float light=clamp(hi*2.1,0.12,0.92);
            c.rgb=vec3(light*0.97,light*0.79,light*0.64);
        }
    }
    COLOR=c;
}
