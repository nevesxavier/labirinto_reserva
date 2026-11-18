using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controla o jogo inteiro: 3 fases, 10 tesouros no total e a interface do jogador.
///
/// Não precisa ser colocado na cena: ele se cria sozinho ao apertar Play,
/// desde que exista um objeto com MovimentoPersonagem (o jogador).
///
/// Fase 1 -> o mapa pintado na cena "labirinto".
/// Fase 2 e 3 -> labirintos gerados por código (GeradorLabirinto).
/// </summary>
[DefaultExecutionOrder(100)]
public class GerenciadorJogo : MonoBehaviour
{
    public static GerenciadorJogo Instancia { get; private set; }

    // ------------------------------------------------------------------
    // Configuração das fases (altere aqui para mudar o jogo)
    // ------------------------------------------------------------------
    private class DadosFase
    {
        public string nome;
        public int itens;               // quantos tesouros tem na fase
        public int primeiroItem;        // número do primeiro tesouro (0 a 9)
        public bool gerada;             // false = mapa da cena (fase 1)
        public int tamanho;             // labirinto gerado: células por lado
        public int semente;
        public float lacos;
        public Color corChao;
        public Color corParede;
        public bool escuro;             // lanterna (visão limitada)
        public Vector2[] posicoes;      // só para a fase 1
    }

    private static readonly DadosFase[] Fases = CriarFases();

    private static DadosFase[] CriarFases()
    {
        DadosFase f1 = new DadosFase();
        f1.nome = "A Fortaleza de Pedra";
        f1.itens = 3;
        f1.primeiroItem = 0;
        f1.gerada = false;
        // Posições dentro do labirinto da cena, alcançáveis a pé pela entrada
        // (conferidas contra o colisor do personagem e as paredes do mapa).
        f1.posicoes = new Vector2[]
        {
            new Vector2(-36.75f, 26.5f),
            new Vector2(9.25f, 24.75f),
            new Vector2(-17.75f, 59f)
        };

        DadosFase f2 = new DadosFase();
        f2.nome = "As Cavernas Geladas";
        f2.itens = 3;
        f2.primeiroItem = 3;
        f2.gerada = true;
        f2.tamanho = 8;
        f2.semente = 11;
        f2.lacos = 0.12f;
        f2.corChao = new Color(0.72f, 0.88f, 1f, 1f);
        f2.corParede = new Color(0.55f, 0.70f, 1f, 1f);

        DadosFase f3 = new DadosFase();
        f3.nome = "O Labirinto das Sombras";
        f3.itens = 4;
        f3.primeiroItem = 6;
        f3.gerada = true;
        f3.tamanho = 12;
        f3.semente = 29;
        f3.lacos = 0.08f;
        f3.corChao = new Color(0.62f, 0.52f, 0.80f, 1f);
        f3.corParede = new Color(0.45f, 0.36f, 0.62f, 1f);
        f3.escuro = true;

        return new DadosFase[] { f1, f2, f3 };
    }

    // Cada fase gerada fica numa posição diferente do mundo, longe do mapa da fase 1
    private const float DistanciaEntreFases = 300f;

    public const int TotalItens = Recursos.TotalTesouros;

    public static int TotalFases
    {
        get { return Fases.Length; }
    }

    // ------------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------------
    private enum Estado { Menu, Jogando, Pausado, FaseConcluida, Vitoria }

    public int FaseAtual { get; private set; }          // 0, 1 ou 2
    public int ColetadosFase { get; private set; }
    public int ColetadosTotal { get; private set; }

    private Estado estado = Estado.Menu;

    private float tempoFase;
    private float tempoConcluidas;      // soma das fases já terminadas
    private readonly bool[] coletado = new bool[TotalItens];

    private MovimentoPersonagem jogador;
    private Rigidbody2D corpoJogador;
    private CameraFollow seguidorCamera;
    private Camera camera2D;
    private InterfaceUsuario interfaceUsuario;
    private AudioSource fonteSom;

    private Vector2 posicaoInicialFase1;
    private GameObject faseGerada;
    private GameObject limitesMundo;
    private readonly List<ItemColetavel> itensAtuais = new List<ItemColetavel>();

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
        for (int i = 0; i < Fases.Length; i++) somaItens += Fases[i].itens;
        if (somaItens != TotalItens)
            Debug.LogError("GerenciadorJogo: as fases somam " + somaItens + " itens, mas o jogo precisa de " + TotalItens + ".");

        // O ItemColetavel só reconhece quem tem a tag "Player"
        jogador.gameObject.tag = "Player";

        corpoJogador = jogador.GetComponent<Rigidbody2D>();
        seguidorCamera = FindFirstObjectByType<CameraFollow>();
        camera2D = Camera.main;
        if (camera2D == null && seguidorCamera != null) camera2D = seguidorCamera.GetComponent<Camera>();
        posicaoInicialFase1 = jogador.transform.position;

        fonteSom = gameObject.AddComponent<AudioSource>();
        fonteSom.playOnAwake = false;
        fonteSom.spatialBlend = 0f;
        AudioListener.volume = PlayerPrefs.GetInt("labirinto_mudo", 0) == 1 ? 0f : 1f;

        int[] itensPorFase = new int[Fases.Length];
        for (int i = 0; i < Fases.Length; i++) itensPorFase[i] = Fases[i].itens;

        interfaceUsuario = gameObject.AddComponent<InterfaceUsuario>();
        interfaceUsuario.Construir(itensPorFase);
        interfaceUsuario.aoInteragir = delegate { Tocar(Recursos.SomClique); };

        PrepararFase(0);
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
            tempoFase += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                Pausar();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab)) interfaceUsuario.AlternarBussola();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Atalho de teste (só no editor): F2 pega todos os tesouros da fase
            if (Input.GetKeyDown(KeyCode.F2)) ColetarTodosDaFase();
#endif

            AtualizarHudContinuo();
        }
        else if (estado == Estado.Pausado && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            Retomar();
        }
    }

    private void LateUpdate()
    {
        // A escuridão da fase 3 acompanha o jogador depois que a câmera se mexeu
        if (interfaceUsuario != null && Fases[FaseAtual].escuro && jogador != null)
            interfaceUsuario.PosicionarEscuridao(camera2D, jogador.transform.position);
    }

    private void AtualizarHudContinuo()
    {
        interfaceUsuario.AtualizarTempo(tempoFase);

        // Bússola: aponta para o tesouro restante mais próximo
        Vector2 posJogador = jogador.transform.position;
        ItemColetavel maisPerto = null;
        float melhor = float.MaxValue;

        for (int i = 0; i < itensAtuais.Count; i++)
        {
            if (itensAtuais[i] == null) continue;

            Vector2 alvo = itensAtuais[i].transform.position;
            float d = (alvo - posJogador).sqrMagnitude;
            if (d < melhor)
            {
                melhor = d;
                maisPerto = itensAtuais[i];
            }
        }

        if (maisPerto == null)
        {
            interfaceUsuario.DefinirBussola(false, 0f, 0f);
            return;
        }

        Vector2 direcao = (Vector2)maisPerto.transform.position - posJogador;
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
        string corpo = "Encontre os " + TotalItens + " tesouros escondidos nas " + TotalFases + " fases.\n\n"
            + "Mover: W A S D ou setas   |   Correr: Shift\n"
            + "Bússola: Tab   |   Som: M   |   Pausa: Esc";
        if (recorde > 0f)
            corpo += "\n\nMelhor tempo total: " + Recursos.FormatarTempo(recorde, true);

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
            "Fase " + (FaseAtual + 1) + " - " + Fases[FaseAtual].nome + "\n"
                + "Tesouros: " + ColetadosTotal + " / " + TotalItens + "\n"
                + "Tempo da fase: " + Recursos.FormatarTempo(tempoFase, false),
            new OpcaoPainel("Continuar  [Enter]", Retomar, KeyCode.Return),
            new OpcaoPainel("Reiniciar fase  [R]", ReiniciarFase, KeyCode.R),
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
        ReiniciarProgresso();
        PrepararFase(0);
        MostrarMenuInicial();
    }

    private void ReiniciarJogo()
    {
        ReiniciarProgresso();
        IniciarFase(0);
    }

    private void ReiniciarProgresso()
    {
        ColetadosTotal = 0;
        tempoConcluidas = 0f;
        for (int i = 0; i < coletado.Length; i++) coletado[i] = false;
    }

    private void ReiniciarFase()
    {
        // Devolve os tesouros que já tinham sido pegos nesta fase
        DadosFase dados = Fases[FaseAtual];
        for (int i = 0; i < dados.itens; i++) coletado[dados.primeiroItem + i] = false;
        ColetadosTotal -= ColetadosFase;

        IniciarFase(FaseAtual);
    }

    private void ProximaFase()
    {
        IniciarFase(FaseAtual + 1);
    }

    private void IniciarFase(int indice)
    {
        PrepararFase(indice);

        estado = Estado.Jogando;
        Time.timeScale = 1f;
        jogador.Congelado = false;
        interfaceUsuario.EsconderPainel();
        interfaceUsuario.ClarearTela(0.6f);
        interfaceUsuario.MostrarBanner("FASE " + (indice + 1), Fases[indice].nome);
    }

    /// <summary>Chamado pelo ItemColetavel quando o jogador pega um tesouro.</summary>
    public void ColetarItem(ItemColetavel item)
    {
        if (estado != Estado.Jogando || item == null) return;

        int indice = item.indice;
        if (indice >= 0 && indice < coletado.Length) coletado[indice] = true;

        ColetadosFase++;
        ColetadosTotal++;
        itensAtuais.Remove(item);

        Efeitos.Explosao(item.transform.position,
            Recursos.CoresItens[Mathf.Clamp(indice, 0, Recursos.CoresItens.Length - 1)],
            jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao);

        Tocar(Recursos.SomColetar);
        AtualizarHud();
        interfaceUsuario.PulsarSlot(indice);
        interfaceUsuario.MostrarAviso(Recursos.NomesItens[Mathf.Clamp(indice, 0, Recursos.NomesItens.Length - 1)] + "!");

        if (ColetadosFase >= Fases[FaseAtual].itens)
            StartCoroutine(ConcluirFase());
    }

    private void ColetarTodosDaFase()
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

    private IEnumerator ConcluirFase()
    {
        estado = Estado.FaseConcluida;
        jogador.Congelado = true;
        interfaceUsuario.DefinirBussola(false, 0f, 0f);

        float tempo = tempoFase;
        tempoConcluidas += tempo;

        string chaveFase = "labirinto_recorde_fase" + FaseAtual;
        float recordeFase = PlayerPrefs.GetFloat(chaveFase, 0f);
        bool novoRecordeFase = recordeFase <= 0f || tempo < recordeFase;
        if (novoRecordeFase)
        {
            recordeFase = tempo;
            PlayerPrefs.SetFloat(chaveFase, tempo);
        }

        bool ultimaFase = FaseAtual >= TotalFases - 1;
        float tempoTotal = tempoConcluidas;
        float recordeTotal = PlayerPrefs.GetFloat("labirinto_recorde_total", 0f);
        bool novoRecordeTotal = false;

        if (ultimaFase)
        {
            novoRecordeTotal = recordeTotal <= 0f || tempoTotal < recordeTotal;
            if (novoRecordeTotal)
            {
                recordeTotal = tempoTotal;
                PlayerPrefs.SetFloat("labirinto_recorde_total", tempoTotal);
            }
        }

        PlayerPrefs.Save();

        // Pequena pausa para as faíscas e o som terminarem
        Tocar(ultimaFase ? Recursos.SomVitoria : Recursos.SomFase);
        yield return new WaitForSecondsRealtime(0.8f);

        Time.timeScale = 0f;

        string linhaRecorde = novoRecordeFase
            ? "NOVO RECORDE!"
            : "Recorde da fase: " + Recursos.FormatarTempo(recordeFase, true);

        if (!ultimaFase)
        {
            estado = Estado.FaseConcluida;
            interfaceUsuario.MostrarPainel(
                "FASE " + (FaseAtual + 1) + " CONCLUÍDA!",
                Fases[FaseAtual].nome + "\n\n"
                    + "Tesouros: " + ColetadosTotal + " / " + TotalItens + "\n"
                    + "Tempo: " + Recursos.FormatarTempo(tempo, true) + "\n"
                    + linhaRecorde,
                new OpcaoPainel("Próxima fase  [Enter]", ProximaFase, KeyCode.Return));
        }
        else
        {
            estado = Estado.Vitoria;
            string linhaTotal = novoRecordeTotal
                ? "NOVO RECORDE TOTAL!"
                : "Melhor tempo total: " + Recursos.FormatarTempo(recordeTotal, true);

            interfaceUsuario.MostrarPainel(
                "VOCÊ VENCEU!",
                "Você encontrou os " + TotalItens + " tesouros nas " + TotalFases + " fases!\n\n"
                    + "Tempo total: " + Recursos.FormatarTempo(tempoTotal, true) + "\n"
                    + linhaTotal,
                new OpcaoPainel("Jogar de novo  [Enter]", ReiniciarJogo, KeyCode.Return),
                new OpcaoPainel("Menu inicial  [Q]", VoltarAoMenu, KeyCode.Q));
        }
    }

    // ------------------------------------------------------------------
    // Montagem de cada fase
    // ------------------------------------------------------------------
    private void PrepararFase(int indice)
    {
        FaseAtual = indice;
        ColetadosFase = 0;
        tempoFase = 0f;

        DadosFase dados = Fases[indice];

        // Limpa o que sobrou da fase anterior
        for (int i = 0; i < itensAtuais.Count; i++)
        {
            if (itensAtuais[i] != null) Destroy(itensAtuais[i].gameObject);
        }
        itensAtuais.Clear();

        if (faseGerada != null)
        {
            Destroy(faseGerada);
            faseGerada = null;
        }

        Vector2 inicio;
        List<Vector2> posicoesItens;

        if (!dados.gerada)
        {
            // Fase 1: usa o mapa que já está na cena
            inicio = posicaoInicialFase1;
            posicoesItens = new List<Vector2>(dados.posicoes);
            GarantirLimitesDoMundo();
        }
        else
        {
            // Fases 2 e 3: labirinto gerado
            Vector3 origem = new Vector3(DistanciaEntreFases * indice, 0f, 0f);
            GeradorLabirinto.Resultado resultado = GeradorLabirinto.Gerar(
                dados.tamanho, dados.tamanho, dados.semente, origem, dados.itens,
                dados.corChao, dados.corParede, dados.lacos);

            faseGerada = resultado.raiz;
            inicio = resultado.posicaoInicial;
            posicoesItens = resultado.posicoesItens;
        }

        CriarItens(dados, posicoesItens);
        PosicionarJogador(inicio);

        interfaceUsuario.DefinirEscuridao(dados.escuro);
        AtualizarHud();
        AtualizarHudContinuo();
        interfaceUsuario.AtualizarTempo(0f);
    }

    private void CriarItens(DadosFase dados, List<Vector2> posicoes)
    {
        for (int i = 0; i < posicoes.Count; i++)
        {
            GameObject go = new GameObject("Tesouro_" + (dados.primeiroItem + i + 1));
            go.transform.position = new Vector3(posicoes[i].x, posicoes[i].y, 0f);

            ItemColetavel item = go.AddComponent<ItemColetavel>();
            item.Configurar(dados.primeiroItem + i, jogador.CamadaOrdenacaoId, jogador.OrdemOrdenacao);
            itensAtuais.Add(item);
        }
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

        // A câmera pula direto para o jogador (senão ela "voaria" até a nova fase)
        if (seguidorCamera != null) seguidorCamera.IrPara(posicao);
    }

    private void AtualizarHud()
    {
        if (interfaceUsuario == null) return;

        DadosFase dados = Fases[FaseAtual];
        interfaceUsuario.AtualizarFase(FaseAtual + 1, TotalFases, dados.nome);
        interfaceUsuario.AtualizarContagem(ColetadosTotal, TotalItens, ColetadosFase, dados.itens);
        interfaceUsuario.AtualizarSlots(coletado, dados.primeiroItem, dados.primeiroItem + dados.itens);
    }

    // Paredes invisíveis em volta do mapa da fase 1, para o jogador não sair para o vazio
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
