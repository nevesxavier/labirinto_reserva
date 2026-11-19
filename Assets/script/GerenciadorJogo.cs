using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controla o jogo inteiro. Tudo acontece no mapa da cena "labirinto", em 3 etapas:
///
///   Etapa 1 - Labirinto de pedra: colete 4 tesouros para abrir o PORTÃO.
///   Etapa 2 - Pontes do lago:     colete 5 tesouros para liberar o TÚNEL DE TELETRANSPORTE selado (canto inferior).
///   Etapa 3 - O túnel de teletransporte leva ao túnel do topo, onde está o TESOURO FINAL (vitória).
///
/// Total: 10 tesouros (4 + 5 + 1). Não precisa ser colocado na cena: ele se cria sozinho
/// ao apertar Play, desde que exista um objeto com MovimentoPersonagem (o jogador).
/// </summary>
public class GerenciadorJogo : MonoBehaviour
{
    public static GerenciadorJogo Instancia { get; private set; }

    // ------------------------------------------------------------------
    // Configuração (altere aqui para mudar o jogo)
    // ------------------------------------------------------------------

    // Quantos tesouros em cada etapa: 4 (abre o portão) + 5 (libera o túnel de teletransporte) + 1 (final) = 10
    public static readonly int[] ItensPorEtapa = { 4, 5, 1 };
    private static readonly int[] PrimeiroItemDaEtapa = { 0, 4, 9 };

    private static readonly string[] NomesEtapas =
    {
        "O Labirinto de Pedra",
        "As Pontes do Lago",
        "O Tesouro Final"
    };

    // Etapa 1: dentro do labirinto de pedra (alcançáveis a pé com o portão fechado)
    private static readonly Vector2[] ItensEtapa1 =
    {
        new Vector2(-36.75f, 26.5f),
        new Vector2(9.25f, 52.5f),
        new Vector2(9.25f, 24.75f),
        new Vector2(-17.75f, 59f)
    };

    // Etapa 2: nas ilhas, pontes e margem do lago (só alcançáveis depois do portão aberto)
    private static readonly Vector2[] ItensEtapa2 =
    {
        new Vector2(-51.1f, 7.2f),      // margem
        new Vector2(-68.2f, 10.2f),     // ilha da casa azul
        new Vector2(-91.95f, 6.5f),     // ilha da esquerda
        new Vector2(-81.2f, -10.6f),    // ponte comprida
        new Vector2(-74.45f, -22.3f)    // ilha da casa vermelha
    };

    // Só usadas se o tilemap "buraco" não for encontrado na cena
    private static readonly Vector2 BuracoEntradaPadrao = new Vector2(-50.2f, -27.4f);
    private static readonly Vector2 BuracoSaidaPadrao = new Vector2(-85.7f, 21.1f);

    // Onde o jogador chega e onde fica o tesouro final, em relação ao túnel de teletransporte do topo
    private static readonly Vector2 ChegadaRelativa = new Vector2(0f, -2.5f);
    private static readonly Vector2 ItemFinalRelativo = new Vector2(3.5f, -1.7f);

    public const int TotalItens = Recursos.TotalTesouros;

    public static int TotalEtapas
    {
        get { return ItensPorEtapa.Length; }
    }

    // ------------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------------
    private enum Estado { Menu, Jogando, Pausado, Cinematica, Vitoria }

    public int EtapaAtual { get; private set; }         // 0, 1 ou 2
    public int ColetadosEtapa { get; private set; }
    public int ColetadosTotal { get; private set; }

    private Estado estado = Estado.Menu;

    private float tempoJogo;
    private bool teletransportou;
    private float proximoAviso;
    private readonly bool[] coletado = new bool[TotalItens];

    private MovimentoPersonagem jogador;
    private Rigidbody2D corpoJogador;
    private CameraFollow seguidorCamera;
    private Transform alvoOriginalCamera;
    private float suavidadeOriginalCamera;
    private InterfaceUsuario interfaceUsuario;
    private AudioSource fonteSom;

    private Vector2 posicaoInicial;
    private GameObject limitesMundo;
    private readonly List<ItemColetavel> itensAtuais = new List<ItemColetavel>();

    // Elementos do mapa
    private Tilemap portaoMapa;
    private TilemapCollider2D portaoColisor;
    private Vector2 centroPortao;
    private float portaoMinY;
    private float portaoMaxY;
    private PortalBuraco portalEntrada;
    private PortalBuraco portalSaida;
    private Vector2 posicaoEntrada;
    private Vector2 posicaoSaida;

    // ------------------------------------------------------------------
    // Inicialização automática
    // ------------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CriarAutomaticamente()
    {
        if (Instancia != null) return;
        if (FindFirstObjectByType<MovimentoPersonagem>() == null) return;

        new GameObject("GerenciadorJogo").AddComponent<GerenciadorJogo>();
    }

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        jogador = FindFirstObjectByType<MovimentoPersonagem>();
        if (jogador == null)
        {
            Debug.LogError("GerenciadorJogo: nenhum objeto com MovimentoPersonagem na cena.");
            enabled = false;
            return;
        }

        int somaItens = 0;
        for (int i = 0; i < ItensPorEtapa.Length; i++) somaItens += ItensPorEtapa[i];
        if (somaItens != TotalItens)
            Debug.LogError("GerenciadorJogo: as etapas somam " + somaItens + " itens, mas o jogo precisa de " + TotalItens + ".");

        // O ItemColetavel só reconhece quem tem a tag "Player"
        jogador.gameObject.tag = "Player";

        corpoJogador = jogador.GetComponent<Rigidbody2D>();
        seguidorCamera = FindFirstObjectByType<CameraFollow>();
        if (seguidorCamera != null)
        {
            if (seguidorCamera.personagem == null) seguidorCamera.personagem = jogador.transform;
            alvoOriginalCamera = seguidorCamera.personagem;
            suavidadeOriginalCamera = seguidorCamera.suavidade;
        }
        posicaoInicial = jogador.transform.position;

        fonteSom = gameObject.AddComponent<AudioSource>();
        fonteSom.playOnAwake = false;
        fonteSom.spatialBlend = 0f;
        AudioListener.volume = PlayerPrefs.GetInt("labirinto_mudo", 0) == 1 ? 0f : 1f;

        interfaceUsuario = gameObject.AddComponent<InterfaceUsuario>();
        interfaceUsuario.Construir(ItensPorEtapa);
        interfaceUsuario.aoInteragir = delegate { Tocar(Recursos.SomClique); };

        ProcurarElementosDoMapa();
        PrepararMundo();
        MostrarMenuInicial();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instancia == this) Instancia = null;
    }

    // ------------------------------------------------------------------
    // Teclado: pausa, bússola, som
    // ------------------------------------------------------------------
    private void Update()
    {
        if (interfaceUsuario == null) return;

        if (Input.GetKeyDown(KeyCode.M)) AlternarSom();

        if (estado == Estado.Jogando)
        {
            tempoJogo += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                Pausar();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab)) interfaceUsuario.AlternarBussola();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Atalho de teste (só no editor): F2 pega todos os tesouros da etapa
            if (Input.GetKeyDown(KeyCode.F2)) ColetarTodosDaEtapa();
#endif

            AtualizarHudContinuo();
        }
        else if (estado == Estado.Pausado && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            Retomar();
        }
    }

    private void AtualizarHudContinuo()
    {
        interfaceUsuario.AtualizarTempo(tempoJogo);

        // Bússola: aponta para o tesouro restante mais próximo...
        Vector2 posJogador = jogador.transform.position;
        Vector2 alvo = Vector2.zero;
        bool temAlvo = false;
        float melhor = float.MaxValue;

        for (int i = 0; i < itensAtuais.Count; i++)
        {
            if (itensAtuais[i] == null) continue;

            Vector2 posicaoItem = itensAtuais[i].transform.position;
            float d = (posicaoItem - posJogador).sqrMagnitude;
            if (d < melhor)
            {
                melhor = d;
                alvo = posicaoItem;
                temAlvo = true;
            }
        }

        // ...ou para o túnel de teletransporte liberado, quando já não há tesouros para pegar
        if (!temAlvo && EtapaAtual == 2 && !teletransportou)
        {
            alvo = posicaoEntrada;
            temAlvo = true;
        }

        if (!temAlvo)
        {
            interfaceUsuario.DefinirBussola(false, 0f, 0f);
            return;
        }

        Vector2 direcao = alvo - posJogador;
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        interfaceUsuario.DefinirBussola(true, angulo, direcao.magnitude);
    }

    private void AlternarSom()
    {
        bool mudo = AudioListener.volume > 0f;
        AudioListener.volume = mudo ? 0f : 1f;
        PlayerPrefs.SetInt("labirinto_mudo", mudo ? 1 : 0);
        PlayerPrefs.Save();

        if (interfaceUsuario != null)
            interfaceUsuario.MostrarAviso(mudo ? "Som desligado" : "Som ligado");

        if (!mudo) Tocar(Recursos.SomClique);
    }

    private void Tocar(AudioClip clip)
    {
        if (fonteSom != null && clip != null) fonteSom.PlayOneShot(clip, 0.8f);
    }

    // ------------------------------------------------------------------
    // Fluxo do jogo
    // ------------------------------------------------------------------
    private void MostrarMenuInicial()
    {
        estado = Estado.Menu;
        Time.timeScale = 0f;
        jogador.Congelado = true;

        float recorde = PlayerPrefs.GetFloat("labirinto_recorde_total", 0f);
        string corpo = "Colete os tesouros do labirinto de pedra para abrir o portão, atravesse as pontes do lago "
            + "e libere o túnel de teletransporte selado: ele leva ao tesouro final!\n\n"
            + "Mover: W A S D ou setas   |   Correr: Shift\n"
            + "Bússola: Tab   |   Som: M   |   Pausa: Esc";
        if (recorde > 0f)
            corpo += "\n\nMelhor tempo: " + Recursos.FormatarTempo(recorde, true);

        interfaceUsuario.MostrarPainel(
            "LABIRINTO",
            corpo,
            new OpcaoPainel("Jogar  [Enter]", ReiniciarJogo, KeyCode.Return));
    }

    private void Pausar()
    {
        estado = Estado.Pausado;
        Time.timeScale = 0f;
        jogador.Congelado = true;

        interfaceUsuario.MostrarPainel(
            "PAUSADO",
            "Etapa " + (EtapaAtual + 1) + " - " + NomesEtapas[EtapaAtual] + "\n"
                + "Tesouros: " + ColetadosTotal + " / " + TotalItens + "\n"
                + "Tempo: " + Recursos.FormatarTempo(tempoJogo, false),
            new OpcaoPainel("Continuar  [Enter]", Retomar, KeyCode.Return),
            new OpcaoPainel("Reiniciar jogo  [R]", ReiniciarJogo, KeyCode.R),
            new OpcaoPainel("Menu inicial  [Q]", VoltarAoMenu, KeyCode.Q));
    }

    private void Retomar()
    {
        estado = Estado.Jogando;
        Time.timeScale = 1f;
        jogador.Congelado = false;
        interfaceUsuario.EsconderPainel();
    }

    private void VoltarAoMenu()
    {
        interfaceUsuario.EsconderPainel();
        PrepararMundo();
        MostrarMenuInicial();
    }

    private void ReiniciarJogo()
    {
        StopAllCoroutines();
        DevolverCameraAoJogador();
        PrepararMundo();

        estado = Estado.Jogando;
        Time.timeScale = 1f;
        jogador.Congelado = false;
        interfaceUsuario.EsconderPainel();
        interfaceUsuario.ClarearTela(0.6f);
        interfaceUsuario.MostrarBanner("ETAPA 1", NomesEtapas[0]);
    }

    /// <summary>Chamado pelo ItemColetavel quando o jogador pega um tesouro.</summary>
    public void ColetarItem(ItemColetavel item)
    {
        if (estado != Estado.Jogando || item == null) return;

        int indice = Mathf.Clamp(item.indice, 0, TotalItens - 1);
        coletado[indice] = true;

        ColetadosEtapa++;
        ColetadosTotal++;
        itensAtuais.Remove(item);

        Efeitos.Explosao(item.transform.position, Recursos.CoresItens[indice],
            jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao);

        Tocar(Recursos.SomColetar);
        AtualizarHud();
        interfaceUsuario.PulsarSlot(indice);
        interfaceUsuario.MostrarAviso(Recursos.NomesItens[indice] + "!");

        if (EtapaAtual == 2)
            StartCoroutine(Vencer());
        else if (ColetadosEtapa >= ItensPorEtapa[EtapaAtual])
            StartCoroutine(EtapaAtual == 0 ? AbrirPortao() : AbrirBuraco());
    }

    private void ColetarTodosDaEtapa()
    {
        List<ItemColetavel> copia = new List<ItemColetavel>(itensAtuais);
        for (int i = 0; i < copia.Count; i++)
        {
            if (copia[i] == null) continue;
            ItemColetavel item = copia[i];
            ColetarItem(item);
            Destroy(item.gameObject);
        }
    }

    /// <summary>Chamado pelos objetos trancados quando o jogador bate neles.</summary>
    public void AvisarBloqueio(int tipo)
    {
        if (estado != Estado.Jogando || interfaceUsuario == null) return;
        if (Time.time < proximoAviso) return;
        proximoAviso = Time.time + 2.5f;

        if (tipo == 0)
        {
            interfaceUsuario.MostrarAviso("O portão está trancado! " + TextoFaltam(ItensPorEtapa[0] - ColetadosEtapa));
        }
        else if (EtapaAtual == 0)
        {
            interfaceUsuario.MostrarAviso("O túnel de teletransporte está selado. Abra o portão primeiro!");
        }
        else
        {
            interfaceUsuario.MostrarAviso("O túnel de teletransporte está selado! " + TextoFaltam(ItensPorEtapa[1] - ColetadosEtapa));
        }
    }

    private static string TextoFaltam(int quantidade)
    {
        if (quantidade < 0) quantidade = 0;
        return quantidade == 1 ? "Falta 1 tesouro." : "Faltam " + quantidade + " tesouros.";
    }

    /// <summary>Chamado pelo túnel de teletransporte liberado quando o jogador entra nele.</summary>
    public void EntrarNoBuraco()
    {
        if (estado != Estado.Jogando || EtapaAtual != 2 || teletransportou) return;
        StartCoroutine(Teletransportar());
    }

    // ------------------------------------------------------------------
    // Sequências: portão, túnel de teletransporte, teletransporte e vitória
    // ------------------------------------------------------------------
    private IEnumerator AbrirPortao()
    {
        estado = Estado.Cinematica;
        jogador.Congelado = true;
        interfaceUsuario.DefinirBussola(false, 0f, 0f);

        EtapaAtual = 1;
        ColetadosEtapa = 0;
        AtualizarHud();

        Tocar(Recursos.SomFase);
        interfaceUsuario.MostrarBanner("PORTÃO DESTRANCADO!", "Ele fica na parede do lado oeste");
        yield return new WaitForSeconds(0.8f);

        // A câmera mostra o portão se abrindo
        yield return StartCoroutine(FocarCamera(centroPortao, 1.5f));

        Tocar(Recursos.SomPortal);
        float t = 0f;
        float proximoBrilho = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.2f;
            if (portaoMapa != null) portaoMapa.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t));

            proximoBrilho -= Time.deltaTime;
            if (proximoBrilho <= 0f)
            {
                proximoBrilho = 0.05f;
                Vector3 ponto = new Vector3(centroPortao.x, Random.Range(portaoMinY, portaoMaxY), 0f);
                Efeitos.Brilho(ponto, new Color(1f, 0.9f, 0.5f), jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao);
            }
            yield return null;
        }

        if (portaoColisor != null) portaoColisor.enabled = false;
        if (portaoMapa != null) portaoMapa.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.6f);

        yield return StartCoroutine(DevolverCamera(1.2f));

        // Agora os tesouros do lago aparecem
        CriarItens(1, ItensEtapa2, true);

        estado = Estado.Jogando;
        jogador.Congelado = false;
    }

    private IEnumerator AbrirBuraco()
    {
        estado = Estado.Cinematica;
        jogador.Congelado = true;
        interfaceUsuario.DefinirBussola(false, 0f, 0f);

        EtapaAtual = 2;
        ColetadosEtapa = 0;
        AtualizarHud();

        Tocar(Recursos.SomFase);
        interfaceUsuario.MostrarBanner("TÚNEL DE TELETRANSPORTE LIBERADO!", "Ele fica no canto inferior do lago");
        yield return new WaitForSeconds(0.8f);

        yield return StartCoroutine(FocarCamera(posicaoEntrada, 1.5f));

        Tocar(Recursos.SomPortal);
        if (portalEntrada != null) portalEntrada.Liberar();
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(DevolverCamera(1.2f));

        estado = Estado.Jogando;
        jogador.Congelado = false;
    }

    private IEnumerator Teletransportar()
    {
        estado = Estado.Cinematica;
        jogador.Congelado = true;
        interfaceUsuario.DefinirBussola(false, 0f, 0f);

        Tocar(Recursos.SomPortal);
        interfaceUsuario.EscurecerTela(0.35f);
        yield return new WaitForSeconds(0.5f);

        teletransportou = true;
        PosicionarJogador(posicaoSaida + ChegadaRelativa);
        if (portalSaida != null) portalSaida.Liberar();
        CriarItemFinal();
        AtualizarHud();

        interfaceUsuario.ClarearTela(0.6f);
        interfaceUsuario.MostrarBanner("O TESOURO FINAL", "Pegue a coroa para vencer!");
        yield return new WaitForSeconds(0.5f);

        estado = Estado.Jogando;
        jogador.Congelado = false;
    }

    private IEnumerator Vencer()
    {
        estado = Estado.Cinematica;
        jogador.Congelado = true;
        interfaceUsuario.DefinirBussola(false, 0f, 0f);

        float tempo = tempoJogo;
        float recorde = PlayerPrefs.GetFloat("labirinto_recorde_total", 0f);
        bool novoRecorde = recorde <= 0f || tempo < recorde;
        if (novoRecorde)
        {
            recorde = tempo;
            PlayerPrefs.SetFloat("labirinto_recorde_total", tempo);
            PlayerPrefs.Save();
        }

        Tocar(Recursos.SomVitoria);
        yield return new WaitForSecondsRealtime(0.9f);

        estado = Estado.Vitoria;
        Time.timeScale = 0f;

        string linhaRecorde = novoRecorde
            ? "NOVO RECORDE!"
            : "Melhor tempo: " + Recursos.FormatarTempo(recorde, true);

        interfaceUsuario.MostrarPainel(
            "VOCÊ VENCEU!",
            "Você abriu o portão, atravessou o lago e encontrou os " + TotalItens + " tesouros!\n\n"
                + "Tempo: " + Recursos.FormatarTempo(tempo, true) + "\n"
                + linhaRecorde,
            new OpcaoPainel("Jogar de novo  [Enter]", ReiniciarJogo, KeyCode.Return),
            new OpcaoPainel("Menu inicial  [Q]", VoltarAoMenu, KeyCode.Q));
    }

    // Leva a câmera até um ponto e espera um pouco
    private IEnumerator FocarCamera(Vector2 ponto, float espera)
    {
        if (seguidorCamera != null)
        {
            GameObject marcador = new GameObject("AlvoDaCamera");
            marcador.transform.position = new Vector3(ponto.x, ponto.y, 0f);
            seguidorCamera.suavidade = 5f;
            seguidorCamera.personagem = marcador.transform;
        }

        yield return new WaitForSeconds(espera);
    }

    private IEnumerator DevolverCamera(float espera)
    {
        DevolverCameraAoJogador();
        yield return new WaitForSeconds(espera);
    }

    private void DevolverCameraAoJogador()
    {
        if (seguidorCamera == null) return;

        Transform marcador = seguidorCamera.personagem;
        seguidorCamera.personagem = alvoOriginalCamera;
        seguidorCamera.suavidade = suavidadeOriginalCamera;

        if (marcador != null && marcador != alvoOriginalCamera) Destroy(marcador.gameObject);
    }

    // ------------------------------------------------------------------
    // Montagem do mundo
    // ------------------------------------------------------------------

    // Acha o portão e os túneis de teletransporte que você pintou na cena
    private void ProcurarElementosDoMapa()
    {
        Tilemap[] mapas = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        Tilemap buraco = null;

        for (int i = 0; i < mapas.Length; i++)
        {
            if (mapas[i].name == "portao") portaoMapa = mapas[i];
            else if (mapas[i].name == "buraco") buraco = mapas[i];
        }

        if (portaoMapa != null)
        {
            // O portão ganha colisor (para trancar) e o aviso de "trancado"
            portaoColisor = portaoMapa.GetComponent<TilemapCollider2D>();
            if (portaoColisor == null) portaoColisor = portaoMapa.gameObject.AddComponent<TilemapCollider2D>();

            AvisoBloqueio aviso = portaoMapa.gameObject.GetComponent<AvisoBloqueio>();
            if (aviso == null) aviso = portaoMapa.gameObject.AddComponent<AvisoBloqueio>();
            aviso.tipo = 0;

            BoundsInt celulas = portaoMapa.cellBounds;
            Vector3 minimo = portaoMapa.CellToWorld(new Vector3Int(celulas.xMin, celulas.yMin, 0));
            Vector3 maximo = portaoMapa.CellToWorld(new Vector3Int(celulas.xMax, celulas.yMax, 0));
            centroPortao = new Vector2((minimo.x + maximo.x) * 0.5f, (minimo.y + maximo.y) * 0.5f);
            portaoMinY = minimo.y;
            portaoMaxY = maximo.y;
        }
        else
        {
            Debug.LogWarning("GerenciadorJogo: tilemap \"portao\" não encontrado na cena.");
            centroPortao = new Vector2(-47.2f, -2.4f);
            portaoMinY = -5.9f;
            portaoMaxY = 1.1f;
        }

        posicaoEntrada = BuracoEntradaPadrao;
        posicaoSaida = BuracoSaidaPadrao;
        if (buraco != null) AcharBuracos(buraco);
        else Debug.LogWarning("GerenciadorJogo: tilemap \"buraco\" não encontrado; usando posições padrão.");
    }

    // O tilemap "buraco" tem dois grupos de tiles: o de baixo é a entrada, o de cima é a saída.
    private void AcharBuracos(Tilemap mapa)
    {
        BoundsInt limites = mapa.cellBounds;
        HashSet<Vector2Int> celulas = new HashSet<Vector2Int>();

        for (int x = limites.xMin; x < limites.xMax; x++)
        {
            for (int y = limites.yMin; y < limites.yMax; y++)
            {
                if (mapa.HasTile(new Vector3Int(x, y, 0))) celulas.Add(new Vector2Int(x, y));
            }
        }

        List<Vector2> centros = new List<Vector2>();
        HashSet<Vector2Int> visitadas = new HashSet<Vector2Int>();

        foreach (Vector2Int inicio in celulas)
        {
            if (visitadas.Contains(inicio)) continue;

            Queue<Vector2Int> fila = new Queue<Vector2Int>();
            fila.Enqueue(inicio);
            visitadas.Add(inicio);

            Vector2 soma = Vector2.zero;
            int quantidade = 0;

            while (fila.Count > 0)
            {
                Vector2Int atual = fila.Dequeue();
                Vector3 mundo = mapa.CellToWorld(new Vector3Int(atual.x, atual.y, 0));
                soma += new Vector2(mundo.x + 0.5f, mundo.y + 0.5f);
                quantidade++;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        Vector2Int vizinho = new Vector2Int(atual.x + dx, atual.y + dy);
                        if (celulas.Contains(vizinho) && !visitadas.Contains(vizinho))
                        {
                            visitadas.Add(vizinho);
                            fila.Enqueue(vizinho);
                        }
                    }
                }
            }

            centros.Add(soma / quantidade);
        }

        if (centros.Count < 2)
        {
            Debug.LogWarning("GerenciadorJogo: esperava 2 túneis de teletransporte no tilemap \"buraco\", achei " + centros.Count + ". Usando posições padrão.");
            return;
        }

        Vector2 maisBaixo = centros[0];
        Vector2 maisAlto = centros[0];
        for (int i = 1; i < centros.Count; i++)
        {
            if (centros[i].y < maisBaixo.y) maisBaixo = centros[i];
            if (centros[i].y > maisAlto.y) maisAlto = centros[i];
        }

        posicaoEntrada = maisBaixo;
        posicaoSaida = maisAlto;
    }

    // Deixa o jogo no começo: portão trancado, túnel de teletransporte selado, só os tesouros da etapa 1
    private void PrepararMundo()
    {
        GarantirLimitesDoMundo();

        for (int i = 0; i < itensAtuais.Count; i++)
        {
            if (itensAtuais[i] != null) Destroy(itensAtuais[i].gameObject);
        }
        itensAtuais.Clear();

        EtapaAtual = 0;
        ColetadosEtapa = 0;
        ColetadosTotal = 0;
        tempoJogo = 0f;
        teletransportou = false;
        for (int i = 0; i < coletado.Length; i++) coletado[i] = false;

        if (portaoMapa != null)
        {
            portaoMapa.gameObject.SetActive(true);
            portaoMapa.color = Color.white;
            if (portaoColisor != null) portaoColisor.enabled = true;
        }

        if (portalEntrada == null)
        {
            GameObject go = new GameObject("BuracoEntrada");
            go.transform.position = new Vector3(posicaoEntrada.x, posicaoEntrada.y, 0f);
            portalEntrada = go.AddComponent<PortalBuraco>();
            portalEntrada.Configurar(jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao, true, true);
        }
        portalEntrada.Selar();

        if (portalSaida == null)
        {
            GameObject go = new GameObject("BuracoSaida");
            go.transform.position = new Vector3(posicaoSaida.x, posicaoSaida.y, 0f);
            portalSaida = go.AddComponent<PortalBuraco>();
            portalSaida.Configurar(jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao, false, false);
        }
        portalSaida.Selar();

        CriarItens(0, ItensEtapa1, false);
        PosicionarJogador(posicaoInicial);

        AtualizarHud();
        AtualizarHudContinuo();
        interfaceUsuario.AtualizarTempo(0f);
    }

    private void CriarItens(int etapa, Vector2[] posicoes, bool comEfeito)
    {
        for (int i = 0; i < posicoes.Length; i++)
        {
            ItemColetavel item = CriarItem(PrimeiroItemDaEtapa[etapa] + i, posicoes[i], 1f);

            if (comEfeito)
                Efeitos.Explosao(item.transform.position, Recursos.CoresItens[item.indice],
                    jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao);
        }
    }

    private void CriarItemFinal()
    {
        ItemColetavel item = CriarItem(PrimeiroItemDaEtapa[2], posicaoSaida + ItemFinalRelativo, 1.6f);
        Efeitos.Explosao(item.transform.position, Recursos.CoresItens[item.indice],
            jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao);
    }

    private ItemColetavel CriarItem(int indice, Vector2 posicao, float escala)
    {
        GameObject go = new GameObject("Tesouro_" + (indice + 1));
        go.transform.position = new Vector3(posicao.x, posicao.y, 0f);

        ItemColetavel item = go.AddComponent<ItemColetavel>();
        item.Configurar(indice, jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao, escala);
        itensAtuais.Add(item);
        return item;
    }

    private void PosicionarJogador(Vector2 posicao)
    {
        Vector3 atual = jogador.transform.position;
        jogador.transform.position = new Vector3(posicao.x, posicao.y, atual.z);

        if (corpoJogador != null)
        {
            corpoJogador.position = posicao;
            corpoJogador.linearVelocity = Vector2.zero;
        }

        // A câmera pula direto para o jogador (senão ela "voaria" até o novo lugar)
        if (seguidorCamera != null) seguidorCamera.IrPara(posicao);
    }

    private string TextoObjetivo()
    {
        if (EtapaAtual == 0) return "Colete os tesouros para abrir o portão";
        if (EtapaAtual == 1) return "Colete os tesouros para liberar o túnel de teletransporte";
        return teletransportou ? "Pegue o tesouro final" : "Entre no túnel de teletransporte brilhante";
    }

    private void AtualizarHud()
    {
        if (interfaceUsuario == null) return;

        int itensEtapa = ItensPorEtapa[EtapaAtual];
        int primeiro = PrimeiroItemDaEtapa[EtapaAtual];

        interfaceUsuario.AtualizarEtapa(EtapaAtual + 1, TotalEtapas, TextoObjetivo());
        interfaceUsuario.AtualizarContagem(ColetadosTotal, TotalItens, ColetadosEtapa, itensEtapa);
        interfaceUsuario.AtualizarSlots(coletado, primeiro, primeiro + itensEtapa);
    }

    // Paredes invisíveis em volta do mapa, para o jogador não sair para o vazio
    private void GarantirLimitesDoMundo()
    {
        if (limitesMundo != null) return;

        Tilemap referencia = null;
        Tilemap[] mapas = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        for (int i = 0; i < mapas.Length; i++)
        {
            if (mapas[i].name == "barreirasLimites") { referencia = mapas[i]; break; }
        }
        if (referencia == null) return;

        BoundsInt celulas = referencia.cellBounds;
        Vector3 minimo = referencia.CellToWorld(new Vector3Int(celulas.xMin, celulas.yMin, 0));
        Vector3 maximo = referencia.CellToWorld(new Vector3Int(celulas.xMax, celulas.yMax, 0));

        limitesMundo = new GameObject("LimitesDoMundo");
        float espessura = 4f;
        float largura = maximo.x - minimo.x;
        float altura = maximo.y - minimo.y;
        float centroX = (maximo.x + minimo.x) * 0.5f;
        float centroY = (maximo.y + minimo.y) * 0.5f;

        CriarParede("Esquerda", new Vector2(minimo.x - espessura * 0.5f, centroY), new Vector2(espessura, altura + espessura * 2f));
        CriarParede("Direita", new Vector2(maximo.x + espessura * 0.5f, centroY), new Vector2(espessura, altura + espessura * 2f));
        CriarParede("Baixo", new Vector2(centroX, minimo.y - espessura * 0.5f), new Vector2(largura + espessura * 2f, espessura));
        CriarParede("Cima", new Vector2(centroX, maximo.y + espessura * 0.5f), new Vector2(largura + espessura * 2f, espessura));
    }

    private void CriarParede(string nome, Vector2 centro, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(limitesMundo.transform, false);
        go.transform.position = new Vector3(centro.x, centro.y, 0f);
        BoxCollider2D colisor = go.AddComponent<BoxCollider2D>();
        colisor.size = tamanho;
    }
}
