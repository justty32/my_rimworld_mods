using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>七維屬性選擇子（G2：MVP 啟用前五維，Intellect/Morale 預留）。</summary>
    public enum OfficerAttribute { Might, Command, Polity, Charisma, Loyalty, Intellect, Morale }

    /// <summary>具名職官層唯一對外 API。所有方法 null-safe：無 registry/參數 null → 回 null/空/false。
    /// 回 record 本體不回 DTO（消費 mod hard-ref 本 DLL 直讀欄位）；API 壟斷寫入路徑
    /// （Create/Assign/Remove/SetAttribute/OffsetOpinion）以保 index 與 opinion 鍵一致。</summary>
    public static class OfficersApi
    {
        private static readonly List<OfficerRecord> Empty = new List<OfficerRecord>();

        // —— 查詢 ——

        /// <summary>host 上全部職官（無 → 空表，永不 null）。</summary>
        public static IReadOnlyList<OfficerRecord> GetOfficers(WorldObject host)
            => WorldComponent_OfficerRegistry.Get()?.For(host) ?? Empty;

        /// <summary>host 上首個符合角色的職官；無 → null。</summary>
        public static OfficerRecord GetOfficer(WorldObject host, OfficerRoleDef role)
        {
            IReadOnlyList<OfficerRecord> list = GetOfficers(host);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].role == role) return list[i];
            }
            return null;
        }

        /// <summary>以 record id 查；無 → null。</summary>
        public static OfficerRecord GetById(int id)
            => WorldComponent_OfficerRegistry.Get()?.ById(id);

        // —— 生命週期 ——

        /// <summary>建 record（不具現 pawn；屬性自 settings 範圍擲定）。
        /// 超過 maxOfficersPerObject 或參數壞 → null。host 可 null＝待命。</summary>
        public static OfficerRecord CreateOfficer(Faction faction, WorldObject host, OfficerRoleDef role)
        {
            OfficerRecord record = WorldComponent_OfficerRegistry.Get()?.Create(faction, host, role);
            if (record != null)
            {
                Raise(OfficerCreated, record, "OfficerCreated");
            }
            return record;
        }

        /// <summary>調動宿主（newHost 可 null＝待命）。維護執行期索引。</summary>
        public static void AssignOfficer(OfficerRecord record, WorldObject newHost)
        {
            if (record == null) return;
            WorldComponent_OfficerRegistry.Get()?.Assign(record, newHost);
        }

        /// <summary>移除 record（含他人 opinion 鍵清理）。pawn 不殺、不收回 forced-keep。</summary>
        public static void RemoveOfficer(OfficerRecord record)
        {
            if (record == null) return;
            WorldComponent_OfficerRegistry.Get()?.Remove(record);
        }

        /// <summary>按需具現（GeneratePawn → KeepForever → inhabitants 橋）。
        /// 回 null＝生成失敗，record 保持輕量態。</summary>
        public static Pawn Materialize(OfficerRecord record)
            => OfficerSpawner.Materialize(record);

        // —— 屬性（讀走欄位即可；提供統一 clamp 0-100 寫入口）——

        public static void SetAttribute(OfficerRecord record, OfficerAttribute attr, int value)
        {
            if (record == null) return;
            value = value < 0 ? 0 : (value > 100 ? 100 : value);
            switch (attr)
            {
                case OfficerAttribute.Might: record.might = value; break;
                case OfficerAttribute.Command: record.command = value; break;
                case OfficerAttribute.Polity: record.polity = value; break;
                case OfficerAttribute.Charisma: record.charisma = value; break;
                case OfficerAttribute.Loyalty: record.loyalty = value; break;
                case OfficerAttribute.Intellect: record.intellect = value; break;
                case OfficerAttribute.Morale: record.morale = value; break;
            }
        }

        public static int GetAttribute(OfficerRecord record, OfficerAttribute attr)
        {
            if (record == null) return 0;
            switch (attr)
            {
                case OfficerAttribute.Might: return record.might;
                case OfficerAttribute.Command: return record.command;
                case OfficerAttribute.Polity: return record.polity;
                case OfficerAttribute.Charisma: return record.charisma;
                case OfficerAttribute.Loyalty: return record.loyalty;
                case OfficerAttribute.Intellect: return record.intellect;
                case OfficerAttribute.Morale: return record.morale;
                default: return 0;
            }
        }

        // —— 關係 ——

        /// <summary>B 軌 a→b 好感度（非對稱）；缺鍵 → A 軌 bias。</summary>
        public static int GetOpinion(OfficerRecord a, OfficerRecord b)
        {
            if (a == null || b == null) return 0;
            return a.opinions.TryGetValue(b.id, out int value) ? value : OpinionEvolver.BiasOf(a, b);
        }

        /// <summary>事件脈衝（戰功/羞辱/賞賜…由消費 mod 定義）；心跳會緩慢回歸 bias。</summary>
        public static void OffsetOpinion(OfficerRecord a, OfficerRecord b, int delta)
        {
            if (a == null || b == null) return;
            a.opinions[b.id] = OpinionEvolver.Clamp(GetOpinion(a, b) + delta);
        }

        /// <summary>A 軌持久關係（結拜/世仇）；兩端未具現則按需具現（G4）。失敗回 false。</summary>
        public static bool AddPersistentRelation(OfficerRecord a, OfficerRecord b, PawnRelationDef def)
            => RelationsUtility_Officers.AddPersistent(a, b, def);

        // —— 事件 hook（主執行緒心跳內同步觸發；訂閱者例外逐一隔離）——

        /// <summary>record 建立後（pawn 尚未具現）。</summary>
        public static event System.Action<OfficerRecord> OfficerCreated;

        /// <summary>pawn 死亡，record 標記 dead；廣播後下一輪心跳清理（G5 遺言窗口）。</summary>
        public static event System.Action<OfficerRecord> OfficerDied;

        /// <summary>宿主消失（null/Destroyed/易主），record 轉待命留存。</summary>
        public static event System.Action<OfficerRecord> OfficerUnassigned;

        internal static void RaiseOfficerDied(OfficerRecord r) => Raise(OfficerDied, r, "OfficerDied");
        internal static void RaiseOfficerUnassigned(OfficerRecord r) => Raise(OfficerUnassigned, r, "OfficerUnassigned");

        private static void Raise(System.Action<OfficerRecord> handlers, OfficerRecord record, string name)
        {
            if (handlers == null) return;
            foreach (System.Delegate d in handlers.GetInvocationList())
            {
                try
                {
                    ((System.Action<OfficerRecord>)d)(record);
                }
                catch (System.Exception e)
                {
                    Log.Warning("[named-officers] " + name + " 訂閱者例外（已隔離）：" + e);
                }
            }
        }
    }
}
