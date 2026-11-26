import AppKit
import CoreText

for weight in ["Regular", "Medium", "Bold"] {
    let url = URL(fileURLWithPath: NSHomeDirectory()+"/Library/Fonts/NotoSansCJKsc-"+weight+".otf")
    CTFontManagerRegisterFontsForURL(url as CFURL, .process, nil)
}

// A native, measured UI specimen board. Coordinates are 1x; render at 2x.
let root = URL(fileURLWithPath: CommandLine.arguments[1])
let manifest = try JSONSerialization.jsonObject(with: Data(contentsOf: root.appendingPathComponent("manifest.json"))) as! [String:String]
struct Theme {
    let key: String, title: String
    let bg: String, panel: String, secondary: String, border: String, text: String, muted: String
    let accent: String, hover: String, pressed: String, disabled: String, band: String
    let states: [String], suppression: String, morale: String, radius: CGFloat
}
let themes = [
    Theme(key:"a",title:"FIELD SLATE / 战地石板",bg:"101713",panel:"18221F",secondary:"26332D",border:"52645A",text:"E3E8DA",muted:"AFBCAF",accent:"C9B478",hover:"35473C",pressed:"111A16",disabled:"6E7B70",band:"111A16",states:["49694B","B9A75F","B17A43","96554E","646963"],suppression:"C87569",morale:"7EACBF",radius:2),
    Theme(key:"b",title:"FIELD LEDGER / 战地簿册",bg:"E7DFCD",panel:"D1C7AE",secondary:"BDB49C",border:"817B67",text:"262C26",muted:"4D564A",accent:"536047",hover:"E3D8BE",pressed:"ABA185",disabled:"757466",band:"30372E",states:["647B58","C5B266","B9814E","A96558","858980"],suppression:"A84F42",morale:"476F85",radius:0),
    Theme(key:"c",title:"NIGHT SIGNAL / 夜战信号",bg:"091117",panel:"111A20",secondary:"1D2A32",border:"4E6066",text:"E4E7DF",muted:"ADB9BE",accent:"D2AB68",hover:"344044",pressed:"0C1419",disabled:"708087",band:"0C1419",states:["526D5B","AF9C55","A77746","965A57","626C70"],suppression:"CB776F",morale:"7CA6BD",radius:3)
]
func color(_ s: String) -> NSColor {
    let v = UInt32(s, radix:16)!
    return NSColor(srgbRed:CGFloat((v>>16)&255)/255,green:CGFloat((v>>8)&255)/255,blue:CGFloat(v&255)/255,alpha:1)
}
func rect(_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ h:CGFloat,_ fill:String,_ stroke:String? = nil,_ radius:CGFloat = 0,_ line:CGFloat = 1) {
    let p = NSBezierPath(roundedRect:NSRect(x:x,y:y,width:w,height:h),xRadius:radius,yRadius:radius)
    color(fill).setFill(); p.fill()
    if let s = stroke {color(s).setStroke();p.lineWidth=line;p.stroke()}
}
func label(_ s:String,_ x:CGFloat,_ y:CGFloat,_ size:CGFloat,_ hex:String,_ weight:String = "Regular",_ width:CGFloat = 1000) {
    let font=NSFont(name:"NotoSansCJKsc-"+weight,size:size)!
    let para=NSMutableParagraphStyle();para.lineBreakMode = .byClipping
    (s as NSString).draw(in:NSRect(x:x,y:y,width:width,height:size*1.7),withAttributes:[.font:font,.foregroundColor:color(hex),.paragraphStyle:para])
}
func line(_ points:[NSPoint],_ hex:String,_ thickness:CGFloat = 1) {
    let p=NSBezierPath();p.move(to:points[0]);for a in points.dropFirst(){p.line(to:a)}
    color(hex).setStroke();p.lineWidth=thickness;p.stroke()
}
func head(_ x:CGFloat,_ y:CGFloat,_ hex:String) {
    color(hex).setFill();NSBezierPath(ovalIn:NSRect(x:x+1,y:y,width:4,height:4)).fill()
    NSBezierPath(roundedRect:NSRect(x:x,y:y+4,width:6,height:4),xRadius:1,yRadius:1).fill()
}
func heads(_ x:CGFloat,_ y:CGFloat,_ hex:String) {for n in 0..<10 {head(x+CGFloat(n)*8,y,hex)}}
func nodes(_ x:CGFloat,_ y:CGFloat,_ hex:String) {
    line([NSPoint(x:x+8,y:y+2),NSPoint(x:x+2,y:y+13),NSPoint(x:x+14,y:y+13),NSPoint(x:x+8,y:y+2)],hex,1)
    for p in [NSPoint(x:x+8,y:y+2),NSPoint(x:x+2,y:y+13),NSPoint(x:x+14,y:y+13)] {
        color(hex).setFill();NSBezierPath(ovalIn:NSRect(x:p.x-2,y:p.y-2,width:4,height:4)).fill()
    }
}
func arrow(_ x:CGFloat,_ y:CGFloat,_ hex:String) {line([NSPoint(x:x,y:y+8),NSPoint(x:x+14,y:y+8),NSPoint(x:x+9,y:y+3),NSPoint(x:x+14,y:y+8),NSPoint(x:x+9,y:y+13)],hex,1.5)}
func button(_ s:String,_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ h:CGFloat,_ t:Theme,_ state:Int = 0) {
    let fills=[t.secondary,t.hover,t.pressed,t.panel]
    rect(x,y,w,h,fills[state],state==2 ? t.accent : t.border,t.radius,state==2 ? 2:1)
    let size:CGFloat = h<22 ? 11:12
    let f=NSFont(name:"NotoSansCJKsc-Medium",size:size)!
    let tw=(s as NSString).size(withAttributes:[.font:f]).width
    label(s,x+(w-tw)/2,y+(h-size*1.45)/2,size,state==3 ? t.disabled:t.text,"Medium",w)
}
func meter(_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ value:CGFloat,_ fill:String,_ t:Theme) {
    rect(x,y,w,4,t.secondary);rect(x,y,w*value,4,fill)
}
// Portrait windows display source art as a normal UI image widget; controls are newly drawn.
func portrait(_ x:CGFloat,_ y:CGFloat,_ tag:String,_ t:Theme,_ art:NSImage) {
    let windows:[String:NSRect] = [
        "a":NSRect(x:1025,y:151,width:120,height:130),
        "b":NSRect(x:1070,y:151,width:135,height:151),
        "c":NSRect(x:540,y:488,width:155,height:160)
    ]
    let window=windows[t.key]!
    let crop=NSRect(x:window.minX,y:art.size.height-window.maxY,width:window.width,height:window.height)
    art.draw(in:NSRect(x:x,y:y,width:76,height:54),from:crop,operation:.sourceOver,fraction:1,respectFlipped:true,hints:nil)
    rect(x,y,28,16,t.band)
    label(tag,x+3,y,11,"E4E7DF","Medium",26)
}
for t in themes {
    let bitmap=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:2000,pixelsHigh:1300,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!
    let ctx=NSGraphicsContext(bitmapImageRep:bitmap)!
    NSGraphicsContext.saveGraphicsState();NSGraphicsContext.current=ctx
    ctx.cgContext.translateBy(x:0,y:1300);ctx.cgContext.scaleBy(x:2,y:-2)
    let flipped=NSGraphicsContext(cgContext:ctx.cgContext,flipped:true);NSGraphicsContext.current=flipped
    let art=NSImage(contentsOf:root.appendingPathComponent(manifest[t.key+"_components"]!))!
    rect(0,0,1000,650,t.bg)
    label(t.key.uppercased()+" / "+t.title,24,16,18,t.text,"Bold")
    label("HUD COMPONENTS · 2×   /   下列尺寸均为 1× 逻辑像素",24,44,11,t.muted)
    line([NSPoint(x:24,y:74),NSPoint(x:976,y:74)],t.border)
    func section(_ s:String,_ x:CGFloat,_ y:CGFloat) {label(s,x,y,11,t.muted,"Medium")}
    section("01 / BUTTON STATES · 72 × 26",24,90)
    for (n,s) in ["正常 / NORMAL","悬停 / HOVER","按下 / PRESSED","禁用 / DISABLED"].enumerated(){
        let y=CGFloat(120+n*38);button("支援 12",24,y,72,26,t,n);label(s,110,y+4,11,t.muted)
    }
    section("02 / NAME BARS · 148 × 24",24,292)
    for (n,s) in ["良好","被压制","动摇","溃散","阵亡"].enumerated(){
        let y=CGFloat(320+n*32);rect(24,y,148,24,t.states[n]);
        let foreground=(n==0 || n==3 || n==4) ? "F0F1E8":"161C18"
        label(s,30,y+2,12,foreground,"Medium");label(["GOOD","PINNED","SHAKEN","BROKEN","GONE"][n],184,y+4,11,t.muted)
    }
    section("03 / METERS · 176 × 4",24,494)
    label("压制 68%",24,516,11,t.text);meter(96,524,176,0.68,t.suppression,t)
    label("士气 82%",24,542,11,t.text);meter(96,550,176,0.82,t.morale,t)
    section("04 / ORDER MENU · 256 W · 24 ROW",350,90)
    rect(350,120,256,146,t.panel,t.border,t.radius)
    label("目标：建筑（3 层）",360,126,13,t.text,"Bold")
    line([NSPoint(x:350,y:152),NSPoint(x:606,y:152)],t.border)
    for (n,s) in ["移动","快速移动","倒车"].enumerated(){
        let y=CGFloat(152+n*24),c=n==2 ? t.disabled:t.text
        if n==1 {rect(351,y,254,24,t.hover)}
        arrow(360,y+4,c);label(s,384,y+2,12,c,"Regular",186);label(["M","R","X"][n],585,y+2,12,c,"Medium",16)
    }
    line([NSPoint(x:360,y:229),NSPoint(x:596,y:229)],t.border)
    nodes(360,241,t.text);label("托管：弹性防御",384,238,12,t.text,"Regular",190);label("Z",585,238,12,t.text)
    section("05 / SELECTED-SQUAD PANEL SAMPLE",350,292)
    rect(350,320,280,144,t.panel,t.border,t.radius)
    if t.key=="c" {rect(350,320,2,32,t.accent)}
    label("1. Gruppe — 步兵班",360,327,14,t.text,"Bold")
    label("伤亡 0% · 士气 68% · 被压制",360,352,12,t.muted)
    label("托管：弹性防御",360,376,12,t.text)
    button("准撤",526,375,44,20,t);button("驳回",576,375,44,20,t)
    line([NSPoint(x:360,y:403),NSPoint(x:620,y:403)],t.border)
    label("下士班长 Friedrich Wagner",360,410,12,t.text,"Medium",252)
    label("MP40 冲锋枪 · 192",360,430,11,t.muted)
    label("镇定",579,410,11,t.text)
    meter(534,437,86,0.18,t.suppression,t);meter(534,446,86,0.82,t.morale,t)
    section("06 / HEAD BAND · 10 MEN · 224 × 30",350,494)
    rect(350,524,224,30,t.band);heads(358,535,"E4E7DF");label("镇定",524,530,11,"E4E7DF")
    section("07 / ROSTER · 224 × 54",660,90)
    portrait(660,120,"弹性",t,art)
    rect(736,120,148,24,"C6A057");label("1. Gruppe",742,122,12,"161C18","Bold")
    rect(736,144,148,30,t.band);heads(740,145,"E4E7DF")
    label("请求撤退",740,156,11,"E4E7DF","Regular",54)
    button("准撤",798,154,38,18,t);button("驳回",840,154,38,18,t)
    color(t.accent).setStroke();let selection=NSBezierPath(rect:NSRect(x:659,y:119,width:226,height:56));selection.lineWidth=1;selection.stroke()
    section("08 / CORNER TAGS · 28 × 16",660,208)
    for (n,tag) in ["弹性","迟滞","死守"].enumerated(){portrait(660+CGFloat(n)*88,238,tag,t,art)}
    section("09 / CURSOR HINT · 224 × 26",660,320)
    rect(660,350,224,26,t.panel,t.border,t.radius);label("移动 — 左键点目标",668,354,12,t.text)
    section("10 / PIXEL SCALE & TYPE",660,410)
    label("Noto Sans CJK SC · 400 / 500 / 700",660,434,11,t.text)
    label("11 / 12 / 13 / 14 px → 22 / 24 / 26 / 28 px",660,456,11,t.muted)
    line([NSPoint(x:660,y:494),NSPoint(x:760,y:494)],t.accent,1)
    line([NSPoint(x:660,y:490),NSPoint(x:660,y:498)],t.accent,1)
    line([NSPoint(x:760,y:490),NSPoint(x:760,y:498)],t.accent,1)
    label("100 logical px = 200 image px",660,508,11,t.muted)
    section("11 / RETREAT TOAST · 690 × 32",24,580)
    rect(24,606,690,32,t.panel,t.border,t.radius);rect(24,606,2,32,t.accent)
    label("1. Zug · 1. Gruppe 请求撤退：20 秒内不驳回就自动撤",34,612,12,t.text,"Medium",670)
    label("U3 / 2026-11-25",852,613,11,t.muted)
    NSGraphicsContext.restoreGraphicsState()
    try bitmap.representation(using:.png,properties:[:])!.write(to:root.appendingPathComponent("components_"+t.key+".png"))
    print("Rendered components_"+t.key+".png at 2000 × 1300; 2× scale")
}
