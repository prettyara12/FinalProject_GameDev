using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// ObstacleNaming: menerjemahkan collider yang ditabrak Rian menjadi nama yang mudah dibaca pemain,
/// misalnya "mobil", "pesawat", atau "Pak Beni" untuk pesan "Anda menabrak ...".
/// Urutan pencarian: penerima paket / NPC -> kata kunci pada nama objek (lalu induknya) -> nama objek yang dirapikan.
/// </summary>
public static class ObstacleNaming
{
    private struct Rule
    {
        public string label;
        public string[] tokens;
        public Rule(string label, params string[] tokens) { this.label = label; this.tokens = tokens; }
    }

    // Urutan penting: aturan yang lebih spesifik (lebih banyak kata) ditaruh lebih dulu.
    private static readonly Rule[] rules =
    {
        new Rule("truk tangga pesawat", "stair", "truck"),
        new Rule("tangga pesawat", "stair"),
        new Rule("jembatan gerbang pesawat", "gangway"),
        new Rule("truk bahan bakar", "fuel", "truck"),
        new Rule("pipa bahan bakar", "fuel", "pipe"),
        new Rule("keran bahan bakar", "fuel", "tap"),
        new Rule("taksi", "taxi"),
        new Rule("bus bandara", "bus"),
        new Rule("mobil penarik pesawat", "tow", "car"),
        new Rule("gerobak penarik", "tow", "cart"),
        new Rule("troli bagasi", "plane", "cart"),
        new Rule("troli", "cart"),
        new Rule("landasan helikopter", "heli", "pad"),
        new Rule("helikopter", "heli"),
        new Rule("helikopter", "rotor"),
        new Rule("jet tempur", "fighter"),
        new Rule("pesawat jet", "jet"),
        new Rule("pesawat", "plane"),
        new Rule("pesawat", "propellor"),
        new Rule("pesawat", "propeller"),
        new Rule("rudal", "missile"),
        new Rule("menara radar", "radar"),
        new Rule("menara bandara", "tower"),
        new Rule("hanggar", "hanger"),
        new Rule("hanggar", "hangar"),
        new Rule("loket tiket", "ticket", "booth"),
        new Rule("atap loket", "ticket", "cover"),
        new Rule("pembatas antrean", "ticket", "divider"),
        new Rule("loket tiket", "ticket"),
        new Rule("landai bagasi", "luggage", "ramp"),
        new Rule("koper", "luggage"),
        new Rule("koper", "suitcase"),
        new Rule("tas bagasi", "bag"),
        new Rule("tong", "barrel"),
        new Rule("kerucut lalu lintas", "roadcone"),
        new Rule("kerucut lalu lintas", "cone"),
        new Rule("pembatas jalan", "barrier"),
        new Rule("pembatas jalan", "divider"),
        new Rule("pembatas jalan", "curb"),
        new Rule("pembatas jalan", "concrete"),
        new Rule("pot tanaman", "planter"),
        new Rule("lemari", "cabnet"),
        new Rule("lemari", "cabinet"),
        new Rule("gudang kecil", "shed"),
        new Rule("kotak logam", "metal", "box"),
        new Rule("kardus", "cardboard"),
        new Rule("kotak", "box"),
        new Rule("papan tanda", "sign"),
        new Rule("tiang kantong angin", "windsock"),
        new Rule("pendingin udara atap", "conditioner"),
        new Rule("atap kaca", "window", "roof"),
        new Rule("pagar", "fence"),
        new Rule("pagar", "pagar"),
        new Rule("tembok", "wall"),
        new Rule("tembok", "dinding"),
        new Rule("gulungan kertas", "rolled", "paper"),
        new Rule("kertas", "paper"),
        new Rule("mobil polisi", "cop"),
        new Rule("ambulans", "ambo"),
        new Rule("ambulans", "ambulance"),
        new Rule("truk", "truck"),
        new Rule("van", "van"),
        new Rule("mobil", "car"),
        new Rule("mobil", "wagon"),
        new Rule("mobil", "ute"),
        new Rule("mobil", "veh"),
        new Rule("mobil", "vehicle"),
        new Rule("pos pemadam kebakaran", "fire", "station"),
        new Rule("mal", "mall"),
        new Rule("apartemen", "apartment"),
        new Rule("kedai kopi", "coffee"),
        new Rule("toko obat", "drug"),
        new Rule("toko donat", "donut"),
        new Rule("kedai burger", "burger"),
        new Rule("kedai es krim", "icecream"),
        new Rule("toko", "shop"),
        new Rule("toko", "store"),
        new Rule("toko", "barber"),
        new Rule("toko", "baber"),
        new Rule("toko", "pawn"),
        new Rule("gedung terminal", "terminal"),
        new Rule("gedung bandara", "building"),
        new Rule("gedung", "bangunan"),
        new Rule("bangku", "bench"),
        new Rule("meja", "table"),
        new Rule("kursi", "chair"),
        new Rule("pohon", "tree"),
        new Rule("pohon", "palm"),
        new Rule("pohon", "pohon"),
        new Rule("semak", "hedge"),
        new Rule("batu", "rock"),
        new Rule("batu", "boulder"),
        new Rule("tempat sampah", "dumpster"),
        new Rule("tempat sampah", "trash"),
        new Rule("tempat sampah", "bin"),
        new Rule("hidran", "hydrant"),
        new Rule("payung", "umbrella"),
        new Rule("tiang lampu", "lamp"),
        new Rule("tiang", "pole"),
        new Rule("tiang", "post"),
        new Rule("pipa", "pipe"),
    };

    // Kata yang tidak informatif saat nama objek dirapikan sebagai cadangan.
    private static readonly HashSet<string> noiseTokens = new HashSet<string>
    {
        "npc", "prefab", "mesh", "items", "item", "prop", "props", "obj", "object", "clone", "model", "airport",
        "seperate", "separate", "base", "main", "root", "static", "dark", "light",
    };

    /// <summary>Nama yang enak dibaca untuk collider yang ditabrak (huruf kecil untuk benda, kapital untuk orang).</summary>
    public static string GetFriendlyName(Collider col)
    {
        if (col == null) return "benda";

        // 1. Penerima paket / NPC bernama
        DeliveryRecipient recipient = col.GetComponentInParent<DeliveryRecipient>();
        if (recipient != null && !string.IsNullOrWhiteSpace(recipient.characterName))
        {
            return StripLevelSuffix(recipient.characterName.Trim());
        }

        NPCWalker walker = col.GetComponentInParent<NPCWalker>();
        if (walker != null && !string.IsNullOrWhiteSpace(walker.characterDisplayName) && walker.characterDisplayName != "NPC")
        {
            return StripLevelSuffix(walker.characterDisplayName.Trim());
        }

        Animator anim = col.GetComponentInParent<Animator>();
        if (anim != null && (col.CompareTag("NPC") || anim.gameObject.name.StartsWith("NPC", System.StringComparison.OrdinalIgnoreCase)))
        {
            string person = PrettyName(anim.gameObject.name);
            return string.IsNullOrEmpty(person) ? "warga" : person;
        }

        // 2. Kata kunci pada nama collider, lalu induknya (maks. 3 tingkat)
        Transform t = col.transform;
        for (int depth = 0; t != null && depth < 4; depth++, t = t.parent)
        {
            HashSet<string> tokens = Tokenize(t.name);
            string label = MatchRule(tokens);
            if (label != null) return label;
        }

        // 3. Cadangan: nama objek yang dirapikan
        string pretty = PrettyName(col.gameObject.name);
        if (string.IsNullOrEmpty(pretty) && col.transform.parent != null) pretty = PrettyName(col.transform.parent.name);
        return string.IsNullOrEmpty(pretty) ? "benda" : pretty.ToLowerInvariant();
    }

    private static string MatchRule(HashSet<string> tokens)
    {
        if (tokens.Count == 0) return null;
        for (int i = 0; i < rules.Length; i++)
        {
            bool all = true;
            for (int k = 0; k < rules[i].tokens.Length; k++)
            {
                if (!tokens.Contains(rules[i].tokens[k])) { all = false; break; }
            }
            if (all) return rules[i].label;
        }
        return null;
    }

    private static string StripLevelSuffix(string name)
    {
        // "Pak Beni 2" (versi bandara) -> "Pak Beni"
        if (name.Length > 2 && name[name.Length - 2] == ' ' && char.IsDigit(name[name.Length - 1]))
        {
            return name.Substring(0, name.Length - 2);
        }
        return name;
    }

    /// <summary>Memecah nama objek menjadi kata huruf kecil (pemisah: simbol, angka, dan huruf besar camelCase).</summary>
    private static HashSet<string> Tokenize(string name)
    {
        var set = new HashSet<string>();
        if (string.IsNullOrEmpty(name)) return set;

        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (!char.IsLetter(c))
            {
                Flush(sb, set);
                continue;
            }
            if (char.IsUpper(c) && sb.Length > 0 && char.IsLower(sb[sb.Length - 1]))
            {
                Flush(sb, set);
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        Flush(sb, set);
        return set;
    }

    private static void Flush(StringBuilder sb, HashSet<string> set)
    {
        if (sb.Length == 0) return;
        string w = sb.ToString();
        sb.Length = 0;
        set.Add(w);
        // bentuk tunggal untuk jamak sederhana: bags -> bag, chairs -> chair
        if (w.Length > 3 && w.EndsWith("s") && !w.EndsWith("ss")) set.Add(w.Substring(0, w.Length - 1));
    }

    /// <summary>"Items_RolledPaper_01 (35)" -> "Rolled Paper".</summary>
    private static string PrettyName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var words = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (!char.IsLetter(c)) { AddWord(sb, words); continue; }
            if (char.IsUpper(c) && sb.Length > 0 && char.IsLower(sb[sb.Length - 1])) AddWord(sb, words);
            sb.Append(c);
        }
        AddWord(sb, words);
        return string.Join(" ", words);
    }

    private static void AddWord(StringBuilder sb, List<string> words)
    {
        if (sb.Length == 0) return;
        string w = sb.ToString();
        sb.Length = 0;
        if (noiseTokens.Contains(w.ToLowerInvariant())) return;
        words.Add(char.ToUpperInvariant(w[0]) + w.Substring(1));
    }
}
