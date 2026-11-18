using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Gera um labirinto com Tilemap usando os tiles da pasta Resources/Tiles
/// (Parede e Chao). Usado nas fases 2 e 3.
/// Cada célula do labirinto tem 3x3 tiles de corredor e 1 tile de parede entre
/// elas, para o personagem (colisor de ~0,9 x 1,4) passar com folga.
/// </summary>
public static class GeradorLabirinto
{
    public class Resultado
    {
        public GameObject raiz;
        public Vector2 posicaoInicial;
        public List<Vector2> posicoesItens = new List<Vector2>();
    }

    // Largura do corredor (em tiles) e distância entre uma célula e a seguinte
    private const int LarguraCorredor = 3;
    private const int Passo = LarguraCorredor + 1; // corredor + 1 tile de parede

    // Tiles de "pedra" em volta do labirinto, para não aparecer o vazio
    private const int Margem = 8;

    /// <param name="lacos">Fração (0 a 1) de paredes extras abertas para criar caminhos alternativos.</param>
    public static Resultado Gerar(int colunas, int linhas, int semente, Vector3 origem, int quantidadeItens,
                                  Color corChao, Color corParede, float lacos)
    {
        TileBase tileParede = Resources.Load<TileBase>("Tiles/Parede");
        TileBase tileChao = Resources.Load<TileBase>("Tiles/Chao");

        if (tileParede == null || tileChao == null)
        {
            Debug.LogError("GeradorLabirinto: tiles não encontrados em Assets/Resources/Tiles (Parede e Chao).");
        }

        // ---------- 1) Sorteia as passagens (backtracker iterativo) ----------
        bool[,] passagemDireita = new bool[colunas, linhas];
        bool[,] passagemCima = new bool[colunas, linhas];
        bool[,] visitada = new bool[colunas, linhas];

        System.Random sorteio = new System.Random(semente);
        Stack<Vector2Int> pilha = new Stack<Vector2Int>();
        List<Vector2Int> vizinhos = new List<Vector2Int>(4);

        pilha.Push(new Vector2Int(0, 0));
        visitada[0, 0] = true;

        while (pilha.Count > 0)
        {
            Vector2Int atual = pilha.Peek();
            vizinhos.Clear();

            if (atual.x + 1 < colunas && !visitada[atual.x + 1, atual.y])
                vizinhos.Add(new Vector2Int(atual.x + 1, atual.y));
            if (atual.x - 1 >= 0 && !visitada[atual.x - 1, atual.y])
                vizinhos.Add(new Vector2Int(atual.x - 1, atual.y));
            if (atual.y + 1 < linhas && !visitada[atual.x, atual.y + 1])
                vizinhos.Add(new Vector2Int(atual.x, atual.y + 1));
            if (atual.y - 1 >= 0 && !visitada[atual.x, atual.y - 1])
                vizinhos.Add(new Vector2Int(atual.x, atual.y - 1));

            if (vizinhos.Count == 0)
            {
                pilha.Pop();
                continue;
            }

            Vector2Int proxima = vizinhos[sorteio.Next(vizinhos.Count)];

            if (proxima.x > atual.x) passagemDireita[atual.x, atual.y] = true;
            else if (proxima.x < atual.x) passagemDireita[proxima.x, proxima.y] = true;
            else if (proxima.y > atual.y) passagemCima[atual.x, atual.y] = true;
            else passagemCima[proxima.x, proxima.y] = true;

            visitada[proxima.x, proxima.y] = true;
            pilha.Push(proxima);
        }

        // ---------- 1b) Abre algumas paredes extras (laços): mais de um caminho ----------
        int extras = Mathf.RoundToInt(colunas * linhas * lacos);
        for (int tentativa = 0; tentativa < extras * 20 && extras > 0; tentativa++)
        {
            int ax = sorteio.Next(colunas);
            int ay = sorteio.Next(linhas);

            if (sorteio.Next(2) == 0)
            {
                if (ax + 1 < colunas && !passagemDireita[ax, ay])
                {
                    passagemDireita[ax, ay] = true;
                    extras--;
                }
            }
            else
            {
                if (ay + 1 < linhas && !passagemCima[ax, ay])
                {
                    passagemCima[ax, ay] = true;
                    extras--;
                }
            }
        }

        // ---------- 2) Converte em tiles (true = chão, false = parede) ----------
        int largura = colunas * Passo + 1;
        int altura = linhas * Passo + 1;
        bool[,] piso = new bool[largura, altura];

        for (int cx = 0; cx < colunas; cx++)
        {
            for (int cy = 0; cy < linhas; cy++)
            {
                int baseX = cx * Passo + 1;
                int baseY = cy * Passo + 1;

                // interior da célula
                for (int dx = 0; dx < LarguraCorredor; dx++)
                    for (int dy = 0; dy < LarguraCorredor; dy++)
                        piso[baseX + dx, baseY + dy] = true;

                // abertura para a célula da direita
                if (passagemDireita[cx, cy])
                    for (int dy = 0; dy < LarguraCorredor; dy++)
                        piso[baseX + LarguraCorredor, baseY + dy] = true;

                // abertura para a célula de cima
                if (passagemCima[cx, cy])
                    for (int dx = 0; dx < LarguraCorredor; dx++)
                        piso[baseX + dx, baseY + LarguraCorredor] = true;
            }
        }

        // ---------- 3) Monta os objetos da fase ----------
        GameObject raiz = new GameObject("Fase_Gerada");
        raiz.transform.position = origem;
        raiz.AddComponent<Grid>();

        // Usa o mesmo material das tilemaps da cena (para receber a luz 2D igual)
        Material material = null;
        TilemapRenderer modelo = Object.FindFirstObjectByType<TilemapRenderer>();
        if (modelo != null) material = modelo.sharedMaterial;

        Tilemap mapaFundo = CriarMapa(raiz.transform, "Fundo", -20, false, material);
        Tilemap mapaChao = CriarMapa(raiz.transform, "Chao", -10, false, material);
        Tilemap mapaParedes = CriarMapa(raiz.transform, "Paredes", -5, true, material);

        mapaFundo.color = new Color(corParede.r * 0.6f, corParede.g * 0.6f, corParede.b * 0.6f, 1f);
        mapaChao.color = corChao;
        mapaParedes.color = corParede;

        List<Vector3Int> posChao = new List<Vector3Int>();
        List<Vector3Int> posParedes = new List<Vector3Int>();
        List<Vector3Int> posFundo = new List<Vector3Int>();

        for (int x = 0; x < largura; x++)
        {
            for (int y = 0; y < altura; y++)
            {
                if (piso[x, y]) posChao.Add(new Vector3Int(x, y, 0));
                else posParedes.Add(new Vector3Int(x, y, 0));
            }
        }

        for (int x = -Margem; x < largura + Margem; x++)
        {
            for (int y = -Margem; y < altura + Margem; y++)
            {
                bool dentro = x >= 0 && x < largura && y >= 0 && y < altura;
                if (!dentro) posFundo.Add(new Vector3Int(x, y, 0));
            }
        }

        PintarTiles(mapaChao, posChao, tileChao);
        PintarTiles(mapaParedes, posParedes, tileParede);
        PintarTiles(mapaFundo, posFundo, tileParede);

        // ---------- 4) Posições do jogador e dos itens ----------
        Resultado resultado = new Resultado();
        resultado.raiz = raiz;
        resultado.posicaoInicial = CentroDaCelula(origem, 0, 0);

        // Distância (em passos) de cada célula até a entrada
        int[,] distancia = new int[colunas, linhas];
        for (int x = 0; x < colunas; x++)
            for (int y = 0; y < linhas; y++)
                distancia[x, y] = -1;

        Queue<Vector2Int> fila = new Queue<Vector2Int>();
        fila.Enqueue(new Vector2Int(0, 0));
        distancia[0, 0] = 0;

        while (fila.Count > 0)
        {
            Vector2Int c = fila.Dequeue();
            int d = distancia[c.x, c.y];

            if (passagemDireita[c.x, c.y] && distancia[c.x + 1, c.y] < 0)
            {
                distancia[c.x + 1, c.y] = d + 1;
                fila.Enqueue(new Vector2Int(c.x + 1, c.y));
            }
            if (c.x > 0 && passagemDireita[c.x - 1, c.y] && distancia[c.x - 1, c.y] < 0)
            {
                distancia[c.x - 1, c.y] = d + 1;
                fila.Enqueue(new Vector2Int(c.x - 1, c.y));
            }
            if (passagemCima[c.x, c.y] && distancia[c.x, c.y + 1] < 0)
            {
                distancia[c.x, c.y + 1] = d + 1;
                fila.Enqueue(new Vector2Int(c.x, c.y + 1));
            }
            if (c.y > 0 && passagemCima[c.x, c.y - 1] && distancia[c.x, c.y - 1] < 0)
            {
                distancia[c.x, c.y - 1] = d + 1;
                fila.Enqueue(new Vector2Int(c.x, c.y - 1));
            }
        }

        // Espalha os itens: cada um fica o mais longe possível da entrada
        // e dos itens já escolhidos.
        List<Vector2Int> referencias = new List<Vector2Int>();
        referencias.Add(new Vector2Int(0, 0));

        for (int k = 0; k < quantidadeItens; k++)
        {
            int melhorPontuacao = -1;
            Vector2Int melhorCelula = new Vector2Int(colunas - 1, linhas - 1);

            for (int x = 0; x < colunas; x++)
            {
                for (int y = 0; y < linhas; y++)
                {
                    Vector2Int candidata = new Vector2Int(x, y);
                    if (referencias.Contains(candidata)) continue;

                    int menorDistancia = int.MaxValue;
                    for (int r = 0; r < referencias.Count; r++)
                    {
                        int dist = Mathf.Abs(referencias[r].x - x) + Mathf.Abs(referencias[r].y - y);
                        if (dist < menorDistancia) menorDistancia = dist;
                    }

                    int pontuacao = menorDistancia * 1000 + Mathf.Max(0, distancia[x, y]);
                    if (pontuacao > melhorPontuacao)
                    {
                        melhorPontuacao = pontuacao;
                        melhorCelula = candidata;
                    }
                }
            }

            referencias.Add(melhorCelula);
            resultado.posicoesItens.Add(CentroDaCelula(origem, melhorCelula.x, melhorCelula.y));
        }

        return resultado;
    }

    // Centro do bloco de chão da célula (cx, cy), em coordenadas do mundo.
    // O chão da célula começa no tile (cx * Passo + 1) e tem LarguraCorredor tiles.
    private static Vector2 CentroDaCelula(Vector3 origem, int cx, int cy)
    {
        float metade = 1f + LarguraCorredor / 2f;
        return new Vector2(origem.x + cx * Passo + metade, origem.y + cy * Passo + metade);
    }

    private static Tilemap CriarMapa(Transform pai, string nome, int ordem, bool comColisor, Material material)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);

        Tilemap mapa = go.AddComponent<Tilemap>();
        TilemapRenderer render = go.AddComponent<TilemapRenderer>();
        render.sortingOrder = ordem;
        if (material != null) render.sharedMaterial = material;

        if (comColisor) go.AddComponent<TilemapCollider2D>();

        return mapa;
    }

    private static void PintarTiles(Tilemap mapa, List<Vector3Int> posicoes, TileBase tile)
    {
        if (tile == null || posicoes.Count == 0) return;

        TileBase[] tiles = new TileBase[posicoes.Count];
        for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;

        mapa.SetTiles(posicoes.ToArray(), tiles);
    }
}
