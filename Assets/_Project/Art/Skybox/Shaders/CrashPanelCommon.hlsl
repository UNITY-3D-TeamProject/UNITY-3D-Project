// 튜토리얼 크래시 스타일 공용 면 디자인. Project/TutorialCrashSkybox(빔)와 Project/CrashPanel(프리팹)이 함께 쓴다.
// 이 파일을 include 하기 전에 Hash11(float), Hash21(float2) 를 정의해야 한다.
#ifndef CRASH_PANEL_COMMON_INCLUDED
#define CRASH_PANEL_COMMON_INCLUDED

// ---------- 빔 (네온 데이터 도시 기둥과 같은 면 구성) ----------
// 빔 = 사각 단면 막대를 길이 방향으로 박스(세그먼트) 단위로 나눈 것.
// 면 = 모서리 선 + 박스 이음선 + 안쪽 패널(빈 칸 / 글자 줄 / 창 격자 / 서버 랙). 선은 화소 크기에 맞춰 안티에일리어싱한다.
// 반투명 앞면 너머로 안쪽 모서리가 비치고, 가까운 빔 4개까지 앞에서부터 겹쳐 그린다. 색은 Linear, 모서리는 HDR(블룸).

#define BEAM_EDGE_WIDTH 0.18
#define BEAM_FOG_DISTANCE 260.0
#define BEAM_ROW_HEIGHT 0.7

float LineAA(float d, float w, float fp)
{
    float k = max(w, fp);
    return saturate(1.0 - (d / k)) * (w / k);
}

// 빔 스타일별 색·투명도
struct BeamLook
{
    float3 body;
    float alpha;
    float3 edge;
    float backEdge;
    float3 textA;
    float3 textB;
    float textBRatio;
    float textStrength;
    float isShaded;     // 1이면 면 방향에 따라 몸체 밝기가 달라진다 (솔리드)
    float3 thresholds;  // 패널 종류 경계: 빈 칸 < x <= 글자 줄 < y <= 창 격자 < z <= 서버 랙
};

// 0 홀로그램 패널(흰·파랑) / 1 네온 원본(시안·마젠타) / 2 솔리드 블록 / 3 와이어프레임 / 4 다크 서버 랙
BeamLook GetBeamLook(int style)
{
    BeamLook look;
    look.body = float3(0.004, 0.012, 0.06);
    look.alpha = 0.55;
    look.edge = float3(1.1, 1.35, 2.0);
    look.backEdge = 0.35;
    look.textA = float3(0.75, 0.85, 1.0);
    look.textB = float3(1.0, 0.7, 0.08);
    look.textBRatio = 0.2;
    look.textStrength = 0.6;
    look.isShaded = 0.0;
    look.thresholds = float3(0.25, 0.68, 0.85);

    if (style == 1)
    {
        look.body = float3(0.01, 0.025, 0.045);
        look.alpha = 0.5;
        look.edge = float3(0.3, 1.7, 2.2);
        look.textA = float3(0.2, 0.95, 1.2);
        look.textB = float3(1.5, 0.3, 1.3);
    }
    else if (style == 2)
    {
        look.body = float3(0.12, 0.25, 0.85);
        look.alpha = 1.0;
        look.edge = float3(1.3, 1.35, 1.5);
        look.backEdge = 0.0;
        look.textA = float3(0.85, 0.9, 1.0);
        look.textStrength = 0.55;
        look.isShaded = 1.0;
    }
    else if (style == 3)
    {
        look.body = float3(0.5, 0.6, 1.0);
        look.alpha = 0.06;
        look.edge = float3(1.4, 1.6, 2.2);
        look.backEdge = 0.6;
        look.textA = float3(0.7, 0.8, 1.0);
        look.textBRatio = 0.15;
        look.textStrength = 0.35;
    }
    else if (style == 4)
    {
        look.body = float3(0.003, 0.006, 0.025);
        look.alpha = 0.88;
        look.edge = float3(0.8, 0.95, 1.5);
        look.backEdge = 0.2;
        look.textA = float3(0.5, 0.6, 0.85);
        look.textB = float3(1.6, 1.1, 0.15);
        look.textBRatio = 0.35;
        look.textStrength = 0.7;
        look.thresholds = float3(0.08, 0.28, 0.55);
    }
    return look;
}

// 빔 한 면의 발광. along: 빔 길이 방향 위치(월드), across: 면 안 가로 위치(0~faceW), fp: 그 거리에서 화소 하나의 월드 크기.
// isBack 이면 안쪽 면이라 모서리만 흐리게 돌려준다. scale = 무늬·선 크기 배율(하늘 1, 프리팹은 작게).
float3 BeamFace(float along, float across, float faceW, float fp, float beamSeed, float faceSeed, float t, bool isBack, BeamLook look, float scale)
{
    float segLength = lerp(6.0, 16.0, Hash11(beamSeed + 3.7)) * scale;
    float segIndex = floor(along / segLength);
    float py = frac(along / segLength) * segLength;
    float px = across;

    // 길이 방향 모서리(데이터 펄스가 흐른다) + 박스 이음선
    float sideDist = min(px, faceW - px);
    float seamDist = min(py, segLength - py);
    float pulseDir = (Hash11(beamSeed + 9.1) > 0.5) ? 1.0 : -1.0;
    float pulsePhase = frac((along / (40.0 * scale)) - (t * 8.0 * pulseDir / 40.0) + Hash11(beamSeed + 4.4));
    float pulse = pow(saturate(pulsePhase), 12.0) * 3.0;
    float edge = (LineAA(sideDist, BEAM_EDGE_WIDTH * scale, fp) * (1.0 + pulse)) + LineAA(seamDist, BEAM_EDGE_WIDTH * scale * 0.8, fp);
    float3 edgeColor = look.edge * edge;
    if (isBack)
    {
        return edgeColor * look.backEdge;
    }

    float segHash = Hash21(float2(segIndex, faceSeed) + (beamSeed * 1.7));
    float3 textColor = (Hash21(float2(segIndex * 0.37, faceSeed) + beamSeed) > (1.0 - look.textBRatio)) ? look.textB : look.textA;

    // 박스 안쪽 패널: 글자 줄은 빔 길이 방향으로 쓰고, 줄은 면 가로로 쌓는다.
    float margin = min(faceW, segLength) * 0.1;
    float inPanel = step(margin, px) * step(px, faceW - margin) * step(margin, py) * step(py, segLength - margin);
    float panelX = py - margin;
    float panelY = px - margin;
    float panelLength = segLength - (2.0 * margin);

    float pattern;
    float average;
    float cellSize;
    if (segHash < look.thresholds.x)
    {
        pattern = 0.0;
        average = 0.0;
        cellSize = 1.0;
    }
    else if (segHash < look.thresholds.y)
    {
        // 글자 줄: 줄마다 길이가 다르고, 일부 박스는 글자가 흘러간다.
        float scroll = (segHash > lerp(look.thresholds.x, look.thresholds.y, 0.5)) ? (t * 0.6 * (0.5 + segHash)) : 0.0;
        float rowCoord = panelY / (BEAM_ROW_HEIGHT * scale);
        float row = floor(rowCoord);
        float rowLocal = frac(rowCoord);
        float charWidth = (BEAM_ROW_HEIGHT * scale) * 0.55;
        float charCoord = (panelX / charWidth) + (scroll * 2.0);
        float column = floor(charCoord);
        float columnLocal = frac(charCoord);
        float lineLength = panelLength * Hash21(float2(row, segIndex) + beamSeed) * 1.2;
        float blink = Hash21(float2((row * 3.1) + column, floor(t * 6.0 * (0.3 + segHash))) + beamSeed);
        float charOn = step(0.28, Hash21(float2(row, column) + (beamSeed * 2.3) + segIndex)) * step(0.04, blink);
        float glyph = step(0.25, rowLocal) * step(rowLocal, 0.7) * step(0.1, columnLocal) * step(columnLocal, 0.85);
        pattern = charOn * glyph * step(panelX, lineLength);
        average = 0.17;
        cellSize = (BEAM_ROW_HEIGHT * scale);
    }
    else if (segHash < look.thresholds.z)
    {
        // 창 격자: 칸마다 켜짐/꺼짐이 천천히 바뀐다.
        float cellWidth = (BEAM_ROW_HEIGHT * scale) * 1.4;
        float2 g = float2(panelX, panelY) / cellWidth;
        float2 gi = floor(g);
        float2 gf = frac(g);
        float on = step(0.45, Hash21(gi + beamSeed + segIndex + floor((t * 0.4) + (Hash21(gi + 0.3) * 10.0))));
        pattern = on * step(0.12, gf.x) * step(gf.x, 0.88) * step(0.12, gf.y) * step(gf.y, 0.88) * 0.6;
        average = 0.18;
        cellSize = cellWidth;
    }
    else
    {
        // 서버 랙 슬롯: 길이 방향 막대 + 끝의 상태등
        float slotHeight = (BEAM_ROW_HEIGHT * scale) * 1.4;
        float slot = floor(panelY / slotHeight);
        float slotLocal = frac(panelY / slotHeight);
        float bar = step(0.2, slotLocal) * step(slotLocal, 0.55) * step(panelX, panelLength - (slotHeight * 1.2)) * 0.35;
        float led = step(panelLength - (slotHeight * 0.6), panelX) * step(0.25, slotLocal) * step(slotLocal, 0.5);
        led *= step(0.5, Hash21(float2(slot, floor((t * 3.0) + (Hash21(float2(slot, 1.0) + beamSeed) * 5.0))) + beamSeed));
        pattern = bar + (led * 2.0);
        average = 0.2;
        cellSize = slotHeight;
    }

    // 멀어서 무늬가 화소보다 작아지면 평균 밝기로 바꿔 지글거림을 막는다.
    float detail = saturate(1.6 - (fp / (cellSize * 0.4)));
    pattern = lerp(average, pattern, detail) * inPanel;
    return edgeColor + (textColor * pattern * look.textStrength);
}

#endif
