# T7 — `OfficersApi` 對外門面（0.5d）

> 消費者：P1 warband-generals（讀武力/統率、關係折扣）、P2 settlement-lords（讀政務/忠誠/魅力、
> GovernanceFactor 自算）、P4 faction-politics（反叛者改用職官 record）。
> 形態：**public static 門面 + static 事件 hook**——鏡像 npc-outposts 兩 hook 慣例
> （`WorldComponent_OutpostSpawner.cs:17/22`），消費 mod hard-ref 本 DLL（01 架構決議），零 Harmony。

**Create:** `Source/OfficersApi.cs`（唯一對外入口；registry 的 internal CRUD 不直接暴露）

```csharp
namespace pas.officers
{
    /// <summary>具名職官層唯一對外 API。所有方法 null-safe：無 registry/參數 null → 回 null/空/false。</summary>
    public static class OfficersApi
    {
        // —— 查詢 ——
        public static IReadOnlyList<OfficerRecord> GetOfficers(WorldObject host);
        public static OfficerRecord GetOfficer(WorldObject host, OfficerRoleDef role); // 首個符合
        public static OfficerRecord GetById(int id);

        // —— 生命週期 ——
        /// <summary>建 record（不具現 pawn）。超過 maxOfficersPerObject 或參數壞 → null。</summary>
        public static OfficerRecord CreateOfficer(Faction faction, WorldObject host, OfficerRoleDef role);
        public static void AssignOfficer(OfficerRecord r, WorldObject newHost);   // host 可 null＝待命
        public static void RemoveOfficer(OfficerRecord r);                         // 含他人 opinion 鍵清理
        /// <summary>按需具現（T5）。回 null＝生成失敗，record 保持輕量態。</summary>
        public static Pawn Materialize(OfficerRecord r);

        // —— 屬性（讀走欄位即可；提供統一 clamp 寫入口）——
        public static void SetAttribute(OfficerRecord r, OfficerAttribute attr, int value); // clamp 0-100
        public static int GetAttribute(OfficerRecord r, OfficerAttribute attr);
        // enum OfficerAttribute { Might, Command, Polity, Charisma, Loyalty, Intellect, Morale }

        // —— 關係 ——
        public static int GetOpinion(OfficerRecord a, OfficerRecord b);            // 缺鍵→BiasOf
        public static void OffsetOpinion(OfficerRecord a, OfficerRecord b, int delta); // 事件脈衝
        public static bool AddPersistentRelation(OfficerRecord a, OfficerRecord b, PawnRelationDef def);

        // —— 事件 hook（static Action，npc-outposts hook 範式；null＝零成本）——
        public static event System.Action<OfficerRecord> OfficerCreated;
        public static event System.Action<OfficerRecord> OfficerDied;        // G5：清理前一輪廣播
        public static event System.Action<OfficerRecord> OfficerUnassigned;  // 宿主消失（T4 Heal 3）
    }
}
```

## 設計要點

- **回 record 本體不回 DTO**：消費 mod hard-ref 本層（01：「跨 mod 型別耦合 → 型別放此 mod」），
  直接讀欄位最省；API 只壟斷**寫入路徑**（Create/Assign/Remove/SetAttribute/OffsetOpinion）
  以保 index 與 opinion 鍵一致性。
- **事件在心跳執行緒（主執行緒）同步觸發**；訂閱者例外要被 try-catch 隔離
  （仿 `WarnOnce` 範式——一個壞消費者不拖垮心跳）。
- **GovernanceFactor／戰力公式不住這層**（02：本身不含玩法）；P2 自己算
  `polity×loyalty`。本層只保證屬性可讀。
- P0 同捆**無狀態 view comp**（G3 決議）：
  `Source/World/WorldObjectComp_OfficersView.cs` + `WorldObjectCompProperties_Officers` —
  `CompInspectStringExtra()` 列出 `GetOfficers(parent)` 的名字/角色，**不持有資料、不 scribe**。
  P0 不注入任何 def（零 Harmony）；P1/P2 把這個 props 注入 RW_Warband/Settlement defs 即得 inspect 顯示。

## 驗證步驟

1. build 過；`OfficersApi` 全 public 成員有 XML doc（消費 mod 的契約文件）。
2. dev action 全程只透過 `OfficersApi`（不碰 registry internal）——API 完備性的自我檢驗。
3. null 轟炸：dev action `API null-safety probe` 對每個方法餵 null → 無 throw、回退值正確。
4. 事件：訂一個 log listener（debug action 內）→ Create/殺 pawn/毀宿主分別觸發對應事件。
