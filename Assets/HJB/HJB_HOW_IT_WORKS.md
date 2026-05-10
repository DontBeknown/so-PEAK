# รายงานการศึกษาระบบ HJB Pathfinding
### Hamilton-Jacobi-Bellman Equation-Based Optimal Path Planning on Terrain Mesh

---

## บทคัดย่อ

รายงานฉบับนี้อธิบายหลักการและขั้นตอนการทำงานของระบบค้นหาเส้นทางที่เหมาะสมที่สุด (Optimal Pathfinding) โดยอาศัยสมการ Hamilton-Jacobi-Bellman (HJB) ซึ่งนำมาประยุกต์ใช้ในสภาพแวดล้อมภูมิประเทศสามมิติ ระบบดังกล่าวประกอบด้วยสามขั้นตอนหลัก ได้แก่ การสร้างพื้นผิวต้นทุน (Cost Surface Construction) การแก้สมการ HJB ด้วยวิธี Fast Sweeping Method และการสืบค้นเส้นทางด้วยการไล่ระดับลง (Gradient Descent Backtracking) ผลลัพธ์ที่ได้คือเส้นทางที่เหมาะสมที่สุดบนภูมิประเทศ โดยคำนึงถึงปัจจัยความชันของพื้นที่ ความเหนื่อยล้าสะสม และโซนความเสี่ยง

---

## 1. บทนำ

### 1.1 ที่มาและความสำคัญ

ปัญหาการค้นหาเส้นทางบนภูมิประเทศ (Terrain Pathfinding) เป็นปัญหาพื้นฐานสำคัญในสาขาการพัฒนาเกมสามมิติและระบบหุ่นยนต์อัตโนมัติ วิธีการค้นหาเส้นทางแบบดั้งเดิม เช่น Dijkstra's Algorithm หรือ A* Search มักอาศัยกราฟแบบไม่ต่อเนื่อง (Discrete Graph) ซึ่งอาจสูญเสียความแม่นยำเมื่อนำไปใช้บนภูมิประเทศที่มีความต่อเนื่อง

สมการ Hamilton-Jacobi-Bellman (HJB) ซึ่งมีรากฐานมาจากทฤษฎีการควบคุมเชิงเหมาะสม (Optimal Control Theory) ให้กรอบคณิตศาสตร์ที่เหมาะสมในการหาเส้นทางที่เหมาะสมที่สุดแบบต่อเนื่อง โดยสามารถรวมปัจจัยพลวัต (Dynamic Factors) เช่น ความเหนื่อยล้าของผู้เดิน และโซนอันตราย เข้าไว้ในสมการได้อย่างเป็นธรรมชาติ

### 1.2 วัตถุประสงค์

1. อธิบายหลักการทางคณิตศาสตร์เบื้องหลังสมการ HJB ในบริบทของการค้นหาเส้นทาง
2. นำเสนอขั้นตอนวิธีการนำ HJB ไปใช้งานจริงในสภาพแวดล้อมเกมสามมิติ
3. อธิบายโครงสร้างข้อมูลและค่าพารามิเตอร์ที่เกี่ยวข้อง

### 1.3 ขอบเขต

รายงานนี้ครอบคลุมเฉพาะระบบ HJB Pathfinding ซึ่งประกอบด้วยไฟล์ต้นฉบับหลักดังต่อไปนี้

| ไฟล์ | บทบาท |
|------|--------|
| `Assets/HJB/Cost/CostSurfaceBuilder.cs` | การสร้างพื้นผิวต้นทุน |
| `Assets/HJB/Pathfind/HJBPathSolver.cs` | การแก้สมการ HJB |
| `Assets/HJB/Pathfind/HJBBacktracker.cs` | การสืบค้นเส้นทาง |
| `Assets/HJB/Terrain/HJBMeshDataProvider.cs` | การจัดการข้อมูลภูมิประเทศ |
| `Assets/HJB/Pathfind/DirectionUtility.cs` | อรรถประโยชน์ทิศทาง 16 ทิศ |

---

## 2. ทฤษฎีพื้นฐาน

### 2.1 สมการ Hamilton-Jacobi-Bellman

สมการ HJB เป็นสมการเชิงอนุพันธ์ย่อย (Partial Differential Equation) ที่เกิดขึ้นจากหลักการเหมาะสมของ Bellman (Bellman's Optimality Principle) ซึ่งกล่าวว่า:

> **เส้นทางที่เหมาะสมที่สุดจากจุด A ไปยังจุด B จะต้องประกอบไปด้วยเส้นทางที่เหมาะสมที่สุดระหว่างจุดย่อยทุกจุดตลอดเส้นทาง**

ในบริบทของการค้นหาเส้นทาง เราสนใจฟังก์ชันมูลค่า (Value Function) `T(x, y)` ซึ่งแทนเวลาขั้นต่ำในการเดินทางจากตำแหน่ง `(x, y)` ไปถึงจุดหมาย โดยสมการ HJB แบบ Eikonal มีรูปแบบ:

```
|∇T(x,y)| = c(x,y)
```

โดยที่ `∇T` คือ gradient ของฟังก์ชัน T และ `c(x,y)` คือต้นทุนต่อหน่วยระยะทาง ณ ตำแหน่ง `(x,y)` เงื่อนไขขอบเขต (Boundary Condition) กำหนดให้ `T(goal) = 0`

### 2.2 แบบจำลองความเร็วของ Tobler

ระบบนี้ใช้ **Tobler's Hiking Function** ซึ่งเป็นแบบจำลองเชิงประจักษ์ที่อธิบายความสัมพันธ์ระหว่างความชันของพื้นที่กับความเร็วในการเดิน:

```
v(s) = v₀ × exp(-3.5 × |s + 0.05|)
```

โดยที่ `v₀` คือความเร็วบนพื้นราบ และ `s` คือความชัน (slope) ฟังก์ชันนี้มีลักษณะสำคัญคือให้ความเร็วสูงสุดที่ความชันประมาณ `-0.05` (ลาดเล็กน้อย) และลดลงแบบ exponential เมื่อความชันเพิ่มขึ้นในทั้งสองทิศทาง

---

## 3. ระเบียบวิธี

ระบบทำงานตามลำดับขั้นตอนสามส่วนดังแสดงในแผนภาพต่อไปนี้:

```
[ข้อมูลภูมิประเทศ (heightMap, slopeMap)]
            |
            ▼
[ขั้นที่ 1: สร้าง Cost Surface]
    baseSpeed[x,y], baseCost[x,y]
            |
            ▼
[ขั้นที่ 2: แก้สมการ HJB → T-Field]
    T[x,y] = เวลาขั้นต่ำถึงเป้าหมาย
            |
            ▼
[ขั้นที่ 3: Backtrack จาก T-Field]
    path = รายการตำแหน่ง Vector3
```

### 3.1 ขั้นที่ 1 — การสร้างพื้นผิวต้นทุน (Cost Surface Construction)

**ที่มา:** `Assets/HJB/Cost/CostSurfaceBuilder.cs`

ขั้นตอนแรกแปลงข้อมูลความชันของภูมิประเทศให้กลายเป็นค่าความเร็วและต้นทุนต่อหน่วยระยะทางสำหรับแต่ละช่องกริด โดยประยุกต์ใช้ Tobler's Hiking Function

```
FUNCTION BuildCostSurface(slopeMap):

    FOR each cell (x, y) on the map:

        slope = slopeMap[x, y]          -- how steep is this cell? (0 = flat, 1 = very steep)

        -- Tobler's Hiking Function
        -- exp() means "e to the power of something"
        -- The steeper the slope, the more negative the exponent, the slower the speed
        speed = BASE_SPEED_FLAT * exp(-3.5 * |slope + 0.05|)

        -- Never let speed go to zero (would cause divide-by-zero later)
        speed = max(speed, MIN_SPEED)

        baseSpeed[x, y] = speed
        baseCost[x, y]  = 1 / speed     -- cost per metre = inverse of speed
                                         -- fast cell = cheap, slow cell = expensive

    END FOR
```

ความสัมพันธ์ระหว่างค่าความชันและต้นทุนสรุปได้ดังนี้: ช่องที่มีความชันต่ำจะมีความเร็วสูงและต้นทุนต่ำ ในขณะที่ช่องที่มีความชันสูงจะมีความเร็วต่ำและต้นทุนสูง ตัวอย่างเช่น ช่องพื้นราบซึ่งมี `speed = 5.0` จะมี `cost = 0.2` ในขณะที่ช่องพื้นชันซึ่งมี `speed = 0.3` จะมี `cost = 3.3` คิดเป็นต้นทุนสูงกว่าถึงกว่าสิบหกเท่า

**ค่าพารามิเตอร์:**

| พารามิเตอร์ | ค่า | คำอธิบาย |
|-------------|-----|-----------|
| `BASE_SPEED_FLAT` | 5.0 | ความเร็วอ้างอิงบนพื้นราบ (m/s) |
| `MIN_SPEED` | 0.05 | ค่าความเร็วขั้นต่ำเพื่อป้องกันการหารด้วยศูนย์ |

### 3.2 ขั้นที่ 2 — การแก้สมการ HJB (HJB Solver)

**ที่มา:** `Assets/HJB/Pathfind/HJBPathSolver.cs`

ขั้นตอนนี้คำนวณฟังก์ชันมูลค่า `T[x, y]` ซึ่งแทนเวลาขั้นต่ำในการเดินทางจากทุกช่องกริดไปยังจุดหมาย โดยอาศัย **Fast Sweeping Method (FSM)** ซึ่งเป็นขั้นตอนวิธีที่มีประสิทธิภาพสูงสำหรับการแก้สมการ Eikonal

#### 3.2.1 การกำหนดค่าเริ่มต้น

```
FUNCTION Solve(goal):

    -- Give every cell an impossibly large time (we have not figured it out yet)
    FOR each cell (x, y):
        T[x, y]       = INFINITY
        fatigue[x, y] = 0
    END FOR

    -- The goal itself takes zero time to reach the goal (you are already there)
    T[goal.x, goal.y] = 0
```

กำหนดให้ `T[goal] = 0` เป็นเงื่อนไขขอบเขต และ `T = ∞` สำหรับทุกช่องอื่น แสดงว่ายังไม่ทราบเวลาขั้นต่ำ

#### 3.2.2 วิธี Fast Sweeping

FSM ทำงานโดยทำซ้ำ (iterate) การสแกนแผนที่ใน 4 ทิศทางทแยงมุมสลับกัน ทำให้ข้อมูลเวลาแพร่กระจายออกจากจุดหมายไปยังทุกพื้นที่บนแผนที่อย่างมีประสิทธิภาพ วิธีนี้รับประกันการ converge ภายในจำนวนรอบน้อยกว่า 10 รอบโดยทั่วไป แทนที่จะต้องใช้หลายพันรอบเหมือนวิธีดั้งเดิม

```
    FOR iteration = 1 to MAX_ITERATIONS (50):

        maxChange = 0

        -- Sweep 1: bottom-left to top-right  (x increases, y increases)
        -- Sweep 2: bottom-right to top-left  (x increases, y decreases)
        -- Sweep 3: top-left to bottom-right  (x decreases, y increases)
        -- Sweep 4: top-right to bottom-left  (x decreases, y decreases)
        FOR each of the 4 sweep directions:
            FOR each cell (x, y) in this sweep order:

                oldT = T[x, y]
                UpdateCell(x, y)
                maxChange = max(maxChange, |T[x,y] - oldT|)

            END FOR
        END FOR

        -- If nothing changed much, we have converged -- stop early
        IF maxChange < TOLERANCE:
            BREAK
    END FOR
```

#### 3.2.3 ฟังก์ชัน UpdateCell — สมการ HJB แบบไม่ต่อเนื่อง

ฟังก์ชัน `UpdateCell` นำสมการ HJB มาประยุกต์ใช้แบบ Semi-Lagrangian Discretization โดยตรวจสอบเพื่อนบ้าน 16 ทิศทางและเลือกค่าที่เหมาะสมที่สุด นอกจากต้นทุนพื้นฐานแล้ว ยังรวมปัจจัยความเหนื่อยล้าสะสม (Cumulative Fatigue) และโซนความเสี่ยง (Risk Zone) เข้าในสมการด้วย

```
FUNCTION UpdateCell(x, y):

    bestT = T[x, y]     -- start with current value; only improve it

    FOR each of 16 directions dir:

        -- Step out from (x,y) in this direction
        x2 = x + dir.x * STEP
        y2 = y + dir.y * STEP

        -- Skip if outside the map
        IF out_of_bounds(x2, y2):
            CONTINUE

        -- Skip if this neighbour is too steep to walk on
        IF slope[x2, y2] > MAX_WALKABLE_SLOPE:
            CONTINUE

        -- How long does it take to walk from (x2,y2) to (x,y)?
        -- time = distance / speed
        travel_time = STEP / baseSpeed[x2, y2]

        -- Fatigue: the longer you walk, the more tired you get
        -- Tired walkers move slower (penalty applied to travel_time)
        f_local = fatigue[x2, y2]
                + FATIGUE_RATE_TIME * travel_time
                + FATIGUE_RATE_ELEV * |slope[x2, y2]|

        IF f_local > FATIGUE_LIMIT:
            travel_time = travel_time + FATIGUE_PENALTY * (f_local - FATIGUE_LIMIT)
        END IF

        -- Cost includes the base cost of the terrain plus any risk zones
        cost_local = baseCost[x2, y2] + riskMap[x2, y2] * RISK_WEIGHT

        -- Candidate: cost to walk from neighbour to here PLUS the neighbour's existing T value
        -- This is the HJB update equation:
        --   "if I can reach the goal from (x2,y2) in T[x2,y2] seconds,
        --    then from (x,y) it would take T[x2,y2] + travel_cost to get there first"
        candidate = cost_local * travel_time + T[x2, y2]

        IF candidate < bestT:
            bestT = candidate
        END IF

    END FOR

    T[x, y] = bestT     -- update with the best (lowest) time found
```

สมการอัปเดตที่เป็นหัวใจสำคัญคือ:

```
candidate = cost_local * travel_time + T[x2, y2]
```

สมการนี้เป็นการนำหลักการเหมาะสมของ Bellman มาใช้โดยตรง กล่าวคือ เวลาขั้นต่ำที่จะถึงเป้าหมายจากจุดปัจจุบันเท่ากับ ต้นทุนในการเดินหนึ่งก้าว บวกกับเวลาขั้นต่ำที่ทราบแล้วจากตำแหน่งปลายทางของก้าวนั้น

**ค่าพารามิเตอร์:**

| พารามิเตอร์ | ค่า | คำอธิบาย |
|-------------|-----|-----------|
| `STEP` | 15.0 | ระยะห่างระหว่างจุดสำรวจ (หน่วยกริด) |
| `TOLERANCE` | 0.5 | เกณฑ์การ converge (วินาที) |
| `MAX_ITERATIONS` | 50 | จำนวนรอบสูงสุด |
| `MAX_WALKABLE_SLOPE` | 0.8 | ค่าความชันสูงสุดที่ผ่านได้ |
| `FATIGUE_RATE_TIME` | 0.12 | อัตราความเหนื่อยล้าต่อวินาที |
| `FATIGUE_RATE_ELEV` | 0.0005 | อัตราความเหนื่อยล้าจากความสูง |
| `FATIGUE_LIMIT` | 0.6 | ขีดจำกัดความเหนื่อยล้าก่อนเกิดบทลงโทษ |
| `FATIGUE_PENALTY` | 5.0 | สัมประสิทธิ์บทลงโทษความเหนื่อยล้าส่วนเกิน |

### 3.3 ขั้นที่ 3 — การสืบค้นเส้นทาง (Path Backtracking)

**ที่มา:** `Assets/HJB/Pathfind/HJBBacktracker.cs`

เมื่อ T-Field ถูกคำนวณสมบูรณ์แล้ว การสืบค้นเส้นทางดำเนินการโดยวิธี Greedy Gradient Descent บน T-Field โดยเริ่มจากจุดเริ่มต้นและเคลื่อนที่ไปยังช่องเพื่อนบ้านที่มีค่า T ต่ำกว่าเสมอ จนกว่าจะถึงจุดหมาย

```
FUNCTION BuildPath(start, goal):

    path = []
    p    = start

    WHILE p != goal AND safety_counter < 10000:

        best     = p
        bestVal  = T[p.x, p.y]

        -- Look in all 16 directions
        FOR each of 16 directions dir:

            neighbour = p + dir * STEP

            IF out_of_bounds(neighbour):
                CONTINUE

            -- Is this neighbour lower (closer to goal) than current best?
            IF T[neighbour.x, neighbour.y] < bestVal:
                bestVal = T[neighbour.x, neighbour.y]
                best    = neighbour
            END IF

        END FOR

        -- If no better neighbour found, we are stuck (should not happen on a valid T-field)
        IF best == p:
            PRINT warning: "Stuck during backtracking"
            BREAK
        END IF

        p = best
        path.append( GridToWorld(p.x, p.y) )   -- convert grid cell to 3D world position

        -- Close enough to snap directly to goal (avoids endless fine-stepping at the end)
        IF distance(p, goal) <= STEP:
            path.append( GridToWorld(goal.x, goal.y) )
            BREAK
        END IF

    END WHILE

    RETURN path     -- list of Vector3 world-space positions
```

ความถูกต้องของวิธีการนี้อาศัยคุณสมบัติของ T-Field ที่ว่า ค่า T ของทุกช่องถูกกำหนดมาจากหลักการเหมาะสมแล้ว ดังนั้นการเคลื่อนที่ในทิศทาง Gradient Descent ของ T จึงรับประกันได้ว่าจะนำไปสู่เส้นทางที่เหมาะสมที่สุดเสมอ

---

## 4. โครงสร้างข้อมูล

ตารางด้านล่างสรุปโครงสร้างข้อมูลหลักที่ใช้ในระบบ

| ชื่อ | ชนิดข้อมูล | ความหมาย |
|------|------------|-----------|
| `heightMap[x, y]` | float[,] | ความสูงของภูมิประเทศ ณ แต่ละช่อง |
| `slopeMap[x, y]` | float[,] | ความชันของภูมิประเทศ ณ แต่ละช่อง |
| `baseSpeed[x, y]` | float[,] | ความเร็วในการเดินผ่านแต่ละช่อง (m/s) |
| `baseCost[x, y]` | float[,] | ต้นทุนต่อเมตรของแต่ละช่อง (= 1/speed) |
| `T[x, y]` | float[,] | **ฟังก์ชันมูลค่า HJB** — เวลาขั้นต่ำถึงเป้าหมาย |
| `fatigue[x, y]` | float[,] | ความเหนื่อยล้าสะสม ณ แต่ละช่อง |
| `directions[16]` | Vector2[16] | เวกเตอร์หน่วยของ 16 ทิศทาง |

---

## 5. สรุปและอภิปรายผล

ระบบ HJB Pathfinding ที่ศึกษาในรายงานนี้มีข้อได้เปรียบหลักสองประการเหนือวิธีการค้นหาเส้นทางแบบดั้งเดิม ประการแรก การคำนวณ T-Field เพียงครั้งเดียวให้เส้นทางที่เหมาะสมที่สุดจากทุกจุดบนแผนที่ไปยังเป้าหมาย ซึ่งมีประโยชน์อย่างยิ่งเมื่อจำเป็นต้องคำนวณเส้นทางสำหรับหลายจุดเริ่มต้น ประการที่สอง ระบบสามารถรวมปัจจัยพลวัตที่ซับซ้อน เช่น ความเหนื่อยล้าสะสมและโซนความเสี่ยง เข้าในกรอบคณิตศาสตร์ได้อย่างเป็นระบบ โดยไม่จำเป็นต้องปรับแต่งขั้นตอนวิธีพื้นฐาน

การใช้ Fast Sweeping Method ทำให้ระบบสามารถ converge ได้ภายในไม่กี่รอบการกวาด ส่งผลให้เวลาการคำนวณลดลงจากกว่า 15 นาทีเหลือน้อยกว่า 10 วินาที บนกริดขนาดประมาณ 1,100 × 1,100 ช่อง

ข้อจำกัดที่ควรพิจารณาได้แก่ การที่แบบจำลอง Semi-Lagrangian ใช้ค่า `STEP` คงที่ในการสำรวจเพื่อนบ้าน ซึ่งอาจทำให้เส้นทางที่ได้ไม่ต่อเนื่องอย่างสมบูรณ์แบบในพื้นที่ที่มีการเปลี่ยนแปลงความชันสูง และค่า `TOLERANCE = 0.5` วินาทีเป็นการผ่อนคลาย (Relaxation) จากความเที่ยงตรงทางคณิตศาสตร์เพื่อให้เหมาะสมกับการใช้งานในเกม

---

## อ้างอิง

- Bellman, R. (1957). *Dynamic Programming*. Princeton University Press.
- Sethian, J.A. (1996). A fast marching level set method for monotonically advancing fronts. *Proceedings of the National Academy of Sciences*, 93(4), 1591–1595.
- Tobler, W. (1993). *Three Presentations on Geographical Analysis and Modeling*. National Center for Geographic Information and Analysis.
- Zhao, H. (2005). A fast sweeping method for Eikonal equations. *Mathematics of Computation*, 74(250), 603–627.
