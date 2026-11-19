using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recursos visuais e sonoros do jogo criados POR CÓDIGO (não precisam de arquivos):
/// ícones pixel-art dos 10 tesouros, sprites da interface e efeitos sonoros.
/// </summary>
public static class Recursos
{
    // ------------------------------------------------------------------
    // Os 10 tesouros
    // ------------------------------------------------------------------
    public const int TotalTesouros = 10;

    public static readonly string[] NomesItens =
    {
        "Moeda de Ouro",
        "Chave Antiga",
        "Poção Vermelha",
        "Cristal Azul",
        "Pergaminho",
        "Anel Mágico",
        "Livro Antigo",
        "Estrela Dourada",
        "Coração Rubi",
        "Coroa Real"
    };

    // Cor de brilho/partículas de cada tesouro
    public static readonly Color[] CoresItens =
    {
        new Color(1.00f, 0.82f, 0.25f), // Moeda
        new Color(1.00f, 0.80f, 0.30f), // Chave
        new Color(1.00f, 0.35f, 0.35f), // Poção
        new Color(0.40f, 0.70f, 1.00f), // Cristal
        new Color(1.00f, 0.90f, 0.60f), // Pergaminho
        new Color(0.55f, 0.80f, 1.00f), // Anel
        new Color(1.00f, 0.45f, 0.45f), // Livro
        new Color(1.00f, 0.95f, 0.45f), // Estrela
        new Color(1.00f, 0.40f, 0.55f), // Coração
        new Color(1.00f, 0.85f, 0.30f)  // Coroa
    };

    // Paleta dos ícones (letra -> cor). '.' = transparente
    private static readonly Dictionary<char, Color32> Paleta = new Dictionary<char, Color32>
    {
        { 'y', new Color32(255, 213, 74, 255) },  { 'Y', new Color32(201, 139, 22, 255) },
        { 'w', new Color32(255, 255, 255, 255) },
        { 'r', new Color32(232, 69, 69, 255) },   { 'R', new Color32(150, 30, 50, 255) },
        { 'b', new Color32(84, 160, 255, 255) },  { 'B', new Color32(40, 80, 190, 255) },
        { 'c', new Color32(170, 235, 255, 255) },
        { 'g', new Color32(90, 200, 90, 255) },   { 'G', new Color32(40, 120, 60, 255) },
        { 'p', new Color32(170, 100, 230, 255) }, { 'P', new Color32(100, 50, 160, 255) },
        { 'n', new Color32(176, 112, 64, 255) },  { 'N', new Color32(110, 66, 38, 255) },
        { 's', new Color32(215, 220, 230, 255) }, { 'S', new Color32(130, 140, 160, 255) },
        { 'k', new Color32(240, 224, 176, 255) }, { 'K', new Color32(190, 160, 100, 255) },
        { 'm', new Color32(255, 150, 190, 255) }, { 'o', new Color32(255, 150, 50, 255) }
    };

    private static readonly Color32 CorContorno = new Color32(43, 27, 23, 255);

    private static readonly string[][] MapasIcones =
    {
        new string[]
        {
            "....YYYY....",
            "..YYyyyyYY..",
            ".YywwyyyyyY.",
            ".YywyyyyyyY.",
            "YyywyyYYyyyY",
            "YyyyyYyyYyyY",
            "YyyyyYyyYyyY",
            "YyyyyyYYyyyY",
            ".YyyyyyyyyY.",
            ".YyyyyyyyyY.",
            "..YYyyyyYY..",
            "....YYYY...."
        },
        new string[]
        {
            "...yyyyyy...",
            "..yYYYYYYy..",
            ".yYy....yYy.",
            ".yY......Yy.",
            ".yYy....yYy.",
            "..yYYYYYYy..",
            "....yYYy....",
            ".....yY.....",
            ".....yyyy...",
            ".....yY.....",
            ".....yyyy...",
            ".....yY....."
        },
        new string[]
        {
            "....nnnn....",
            "....nNNn....",
            ".....ss.....",
            ".....ss.....",
            "....s..s....",
            "...s....s...",
            "..srrrrrrs..",
            ".srrwrrrrrs.",
            ".srwrrrrrrs.",
            ".srrrrrrrRs.",
            "..srrrrrRs..",
            "...ssssss..."
        },
        new string[]
        {
            "...bbbbbb...",
            "..bcwwbbbB..",
            ".bcwbbbbbbB.",
            "bbwbbbbbbbBB",
            "bbbbbbbbbbBB",
            ".bbbbbbbbbB.",
            "..bbbbbbbB..",
            "...bbbbbB...",
            "....bbbB....",
            ".....bBB....",
            "......B....."
        },
        new string[]
        {
            ".nnnnnnnnnn.",
            "nNnnnnnnnnNn",
            ".kkkkkkkkkk.",
            ".kKKKKKKKKk.",
            ".kkkkkkkkkk.",
            ".kKKKKKKkkk.",
            ".kkkkkkkkkk.",
            ".kKKKKKKKKk.",
            ".kkkkkkkkkk.",
            "nNnnnnnnnnNn",
            ".nnnnnnnnnn."
        },
        new string[]
        {
            ".....bb.....",
            "....bcwb....",
            ".....BB.....",
            "...yyyyyy...",
            "..yY....Yy..",
            ".yY......Yy.",
            ".yY......Yy.",
            ".yY......Yy.",
            "..yY....Yy..",
            "...yYYYYy..."
        },
        new string[]
        {
            ".rrrrrrrrr..",
            "rRrrrrrrrrr.",
            "rRrryyyrrrr.",
            "rRrryyyrrrr.",
            "rRrrrrrrrrr.",
            "rRrrrrrrrrr.",
            "rRkkkkkkkkr.",
            "rRkKkKkKkKr.",
            ".RkkkkkkkkR.",
            "..RRRRRRRRR."
        },
        new string[]
        {
            ".....yy.....",
            ".....yy.....",
            "....yyyy....",
            "yyyyyyyyyyyy",
            ".yyyywyyyyy.",
            "..yywyyyyy..",
            "...yyyyyy...",
            "..yyyyyyyy..",
            "..yyy..yyy..",
            ".yyy....yyy.",
            ".yy......yy."
        },
        new string[]
        {
            ".rrrr..rrrr.",
            "rrwrrrrrrrrr",
            "rwrrrrrrrrrR",
            "rrrrrrrrrrrR",
            ".rrrrrrrrrR.",
            "..rrrrrrrR..",
            "...rrrrrR...",
            "....rrrR....",
            ".....rR....."
        },
        new string[]
        {
            "y....y....y.",
            "yy..yyy..yy.",
            "yyy.yyy.yyy.",
            "yyyyyyyyyyyy",
            "yyrYyybYyryy",
            "yyyyyyyyyyyy",
            "YYYYYYYYYYYY",
            "YYYYYYYYYYYY"
        }
    };

    // ------------------------------------------------------------------
    // Sprites (criados uma vez e guardados)
    // ------------------------------------------------------------------
    private static Sprite[] icones;
    private static Sprite spriteQuadrado;
    private static Sprite spriteCirculo;
    private static Sprite spriteArredondado;
    private static Sprite spriteSelo;
    private static Sprite spriteSeta;

    /// <summary>Ícone pixel-art do tesouro (0 a 9), com contorno escuro. 14x14 px, 16 px = 1 unidade.</summary>
    public static Sprite Icone(int indice)
    {
        if (icones == null) icones = new Sprite[MapasIcones.Length];
        if (indice < 0 || indice >= MapasIcones.Length) indice = 0;
        if (icones[indice] == null) icones[indice] = CriarIcone(MapasIcones[indice]);
        return icones[indice];
    }

    private static Sprite CriarIcone(string[] mapa)
    {
        const int tam = 14;
        int altura = mapa.Length;
        int largura = 0;
        for (int i = 0; i < mapa.Length; i++)
            if (mapa[i].Length > largura) largura = mapa[i].Length;

        int margemX = (tam - largura) / 2;
        int margemTopo = (tam - altura) / 2;

        Color32[] pixels = new Color32[tam * tam];
        bool[,] ocupado = new bool[tam, tam];

        for (int linha = 0; linha < altura; linha++)
        {
            for (int col = 0; col < mapa[linha].Length; col++)
            {
                Color32 cor;
                if (!Paleta.TryGetValue(mapa[linha][col], out cor)) continue;

                int x = margemX + col;
                int y = tam - 1 - (margemTopo + linha); // textura: y=0 é embaixo
                pixels[y * tam + x] = cor;
                ocupado[x, y] = true;
            }
        }

        // Contorno escuro em volta de todo o desenho
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                if (ocupado[x, y]) continue;

                bool vizinho =
                    (x > 0 && ocupado[x - 1, y]) || (x < tam - 1 && ocupado[x + 1, y]) ||
                    (y > 0 && ocupado[x, y - 1]) || (y < tam - 1 && ocupado[x, y + 1]);

                if (vizinho) pixels[y * tam + x] = CorContorno;
            }
        }

        Texture2D textura = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.SetPixels32(pixels);
        textura.Apply();

        return Sprite.Create(textura, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), 16f);
    }

    /// <summary>Quadradinho branco (partículas).</summary>
    public static Sprite Quadrado()
    {
        if (spriteQuadrado != null) return spriteQuadrado;

        Texture2D textura = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        textura.SetPixels32(pixels);
        textura.Apply();

        spriteQuadrado = Sprite.Create(textura, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return spriteQuadrado;
    }

    /// <summary>Círculo branco com borda suave (brilho dos itens e fundo da bússola).</summary>
    public static Sprite Circulo()
    {
        if (spriteCirculo != null) return spriteCirculo;

        const int tam = 64;
        Texture2D textura = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Bilinear;
        textura.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[tam * tam];
        float centro = (tam - 1) * 0.5f;
        float raio = tam * 0.5f - 1f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                float alfa = Mathf.Clamp01(raio - d + 0.5f); // borda com 1 px suave
                pixels[y * tam + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();

        spriteCirculo = Sprite.Create(textura, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), 100f);
        return spriteCirculo;
    }

    /// <summary>Retângulo de cantos arredondados para usar como Image "Sliced".</summary>
    public static Sprite RetanguloArredondado()
    {
        if (spriteArredondado != null) return spriteArredondado;

        const int tam = 48;
        const int raio = 14;
        Texture2D textura = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Bilinear;
        textura.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                // distância até o "miolo" do retângulo (sem os cantos)
                float cx = Mathf.Clamp(x + 0.5f, raio, tam - raio);
                float cy = Mathf.Clamp(y + 0.5f, raio, tam - raio);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float alfa = Mathf.Clamp01(raio - d + 0.5f);
                pixels[y * tam + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();

        spriteArredondado = Sprite.Create(textura, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(raio, raio, raio, raio));
        return spriteArredondado;
    }

    /// <summary>Selo de pedra com uma runa vermelha, que tampa o túnel de teletransporte enquanto ele está bloqueado (3x3 unidades).</summary>
    public static Sprite Selo()
    {
        if (spriteSelo != null) return spriteSelo;

        const int tam = 48;
        Color32[] pixels = new Color32[tam * tam];
        float centro = (tam - 1) * 0.5f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color32 cor = new Color32(0, 0, 0, 0);

                if (d <= 23f)
                {
                    if (d > 21f) cor = new Color32(43, 27, 23, 255);               // contorno
                    else if (d > 18.5f) cor = new Color32(214, 160, 60, 255);      // aro dourado
                    else
                    {
                        cor = ((x / 3 + y / 3) % 2 == 0)
                            ? new Color32(66, 58, 78, 255)
                            : new Color32(58, 50, 70, 255);                        // pedra

                        bool diagonal = Mathf.Abs(dx - dy) < 2.6f || Mathf.Abs(dx + dy) < 2.6f;
                        if (diagonal && d < 13f) cor = new Color32(226, 70, 70, 255); // runa
                        if (d < 3f) cor = new Color32(255, 213, 74, 255);              // centro
                    }
                }

                pixels[y * tam + x] = cor;
            }
        }

        Texture2D textura = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.SetPixels32(pixels);
        textura.Apply();

        spriteSelo = Sprite.Create(textura, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), 16f);
        return spriteSelo;
    }

    /// <summary>Seta apontando para cima (bússola). Branca com contorno escuro, para receber cor.</summary>
    public static Sprite Seta()
    {
        if (spriteSeta != null) return spriteSeta;

        const int tam = 32;
        bool[,] miolo = new bool[tam, tam];

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = Mathf.Abs(x - (tam - 1) * 0.5f);
                bool ponta = y >= 14 && y <= 28 && dx <= (28 - y) * 0.75f + 0.5f;
                bool haste = y >= 5 && y < 14 && dx <= 3.2f;
                miolo[x, y] = ponta || haste;
            }
        }

        Color32[] pixels = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                if (miolo[x, y])
                {
                    pixels[y * tam + x] = new Color32(255, 255, 255, 255);
                    continue;
                }

                bool vizinho = false;
                for (int ox = -1; ox <= 1 && !vizinho; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;
                        if (nx >= 0 && nx < tam && ny >= 0 && ny < tam && miolo[nx, ny]) { vizinho = true; break; }
                    }
                }

                if (vizinho) pixels[y * tam + x] = new Color32(30, 20, 15, 255);
            }
        }

        Texture2D textura = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.SetPixels32(pixels);
        textura.Apply();

        spriteSeta = Sprite.Create(textura, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), 100f);
        return spriteSeta;
    }

    // ------------------------------------------------------------------
    // Sons (sintetizados, sem arquivos)
    // ------------------------------------------------------------------
    private static AudioClip somColetar;
    private static AudioClip somFase;
    private static AudioClip somVitoria;
    private static AudioClip somClique;
    private static AudioClip somPortal;

    public static AudioClip SomColetar
    {
        get
        {
            if (somColetar == null)
                somColetar = Sequencia("coletar", new float[] { 659.25f, 987.77f }, 0.07f, 0.22f, 0.5f);
            return somColetar;
        }
    }

    public static AudioClip SomFase
    {
        get
        {
            if (somFase == null)
                somFase = Sequencia("fase", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.12f, 0.55f, 0.45f);
            return somFase;
        }
    }

    public static AudioClip SomVitoria
    {
        get
        {
            if (somVitoria == null)
                somVitoria = Sequencia("vitoria",
                    new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f, 1318.5f },
                    0.14f, 0.9f, 0.45f);
            return somVitoria;
        }
    }

    public static AudioClip SomPortal
    {
        get
        {
            if (somPortal == null) somPortal = Varredura("portal", 220f, 880f, 0.9f, 0.4f);
            return somPortal;
        }
    }

    public static AudioClip SomClique
    {
        get
        {
            if (somClique == null)
                somClique = Sequencia("clique", new float[] { 880f }, 0.03f, 0.07f, 0.35f);
            return somClique;
        }
    }

    // Som que sobe de tom suavemente (portão abrindo / teletransporte)
    private static AudioClip Varredura(string nome, float f0, float f1, float duracao, float volume)
    {
        const int taxa = 44100;
        int total = (int)(taxa * duracao);
        float[] dados = new float[total];
        float fase = 0f;

        for (int i = 0; i < total; i++)
        {
            float t = i / (float)total;
            float freq = Mathf.Lerp(f0, f1, t * t);
            fase += 2f * Mathf.PI * freq / taxa;
            float envelope = Mathf.Sin(Mathf.PI * t);
            dados[i] = (Mathf.Sin(fase) * 0.75f + Mathf.Sin(fase * 2f) * 0.15f) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(nome, total, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    private static AudioClip Sequencia(string nome, float[] frequencias, float duracaoNota, float cauda, float volume)
    {
        const int taxa = 44100;
        int total = (int)(taxa * (duracaoNota * frequencias.Length + cauda));
        float[] dados = new float[total];

        for (int n = 0; n < frequencias.Length; n++)
        {
            int inicio = (int)(taxa * duracaoNota * n);
            int tamanho = (int)(taxa * (duracaoNota + cauda));

            for (int i = 0; i < tamanho && inicio + i < total; i++)
            {
                float t = i / (float)taxa;
                float ataque = Mathf.Min(1f, i / (taxa * 0.005f));
                float envelope = Mathf.Exp(-t * 7f) * ataque;
                float onda = Mathf.Sin(2f * Mathf.PI * frequencias[n] * t) * 0.75f
                           + Mathf.Sin(4f * Mathf.PI * frequencias[n] * t) * 0.20f;
                dados[inicio + i] += onda * envelope * volume;
            }
        }

        for (int i = 0; i < total; i++) dados[i] = Mathf.Clamp(dados[i], -1f, 1f);

        AudioClip clip = AudioClip.Create(nome, total, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    // ------------------------------------------------------------------
    // Utilitário: tempo "mm:ss" ou "mm:ss.d"
    // ------------------------------------------------------------------
    public static string FormatarTempo(float segundos, bool comDecimal)
    {
        if (segundos < 0f) segundos = 0f;

        int minutos = (int)(segundos / 60f);
        int resto = (int)(segundos % 60f);
        string texto = (minutos < 10 ? "0" : "") + minutos + ":" + (resto < 10 ? "0" : "") + resto;

        if (comDecimal)
        {
            int decimo = (int)((segundos * 10f) % 10f);
            texto += "." + decimo;
        }

        return texto;
    }
}
