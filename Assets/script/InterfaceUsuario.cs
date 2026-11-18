using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Uma opção (botão) de um painel: texto, ação e tecla de atalho.</summary>
public class OpcaoPainel
{
    public string rotulo;
    public Action acao;
    public KeyCode atalho;

    public OpcaoPainel(string rotulo, Action acao, KeyCode atalho)
    {
        this.rotulo = rotulo;
        this.acao = acao;
        this.atalho = atalho;
    }
}

/// <summary>
/// Interface do jogador, criada inteiramente por código (nada para montar no editor):
///  - HUD: fase, cronômetro, 10 espaços de tesouros, bússola e dicas de controle
///  - avisos ("Moeda coletada!"), faixa de início de fase, fade de tela
///  - escuridão (lanterna) da fase 3
///  - painéis de menu / pausa / fase concluída / vitória
/// </summary>
public class InterfaceUsuario : MonoBehaviour
{
    // ---------------- Paleta ----------------
    private static readonly Color CorPlaca = new Color(0.12f, 0.08f, 0.06f, 0.88f);
    private static readonly Color CorBorda = new Color(0.86f, 0.62f, 0.20f, 1f);
    private static readonly Color CorDestaque = new Color(1f, 0.82f, 0.30f, 1f);
    private static readonly Color CorTexto = new Color(0.98f, 0.94f, 0.82f, 1f);

    private const float TamSlot = 52f;
    private const float EspSlot = 8f;
    private const float EspGrupo = 16f;

    // ---------------- Elementos ----------------
    private Canvas canvas;
    private Sprite spriteArredondado;
    private Sprite spriteCirculo;

    private TextMeshProUGUI textoFase;
    private TextMeshProUGUI textoNomeFase;
    private TextMeshProUGUI textoTempo;
    private TextMeshProUGUI textoContador;
    private TextMeshProUGUI textoFaseItens;

    private Image[] slotFundo;
    private Image[] slotIcone;
    private RectTransform[] slotRect;
    private float[] pulso;
    private const float DuracaoPulso = 0.45f;

    private GameObject bussola;
    private RectTransform setaBussola;
    private TextMeshProUGUI textoDistancia;
    private bool bussolaLigada = true;

    private TextMeshProUGUI textoAviso;
    private float avisoRestante;
    private float avisoDuracao = 1.8f;

    private GameObject banner;
    private TextMeshProUGUI bannerTitulo;
    private TextMeshProUGUI bannerSub;
    private CanvasGroup bannerGrupo;
    private float bannerRestante;
    private float bannerDuracao = 3f;

    private Image fade;
    private float fadeAlfa;
    private float fadeVelocidade;

    private Image escuridao;
    private RectTransform escuridaoRect;

    private GameObject painel;
    private TextMeshProUGUI painelTitulo;
    private TextMeshProUGUI painelCorpo;
    private Button[] botoes = new Button[3];
    private TextMeshProUGUI[] rotulosBotoes = new TextMeshProUGUI[3];
    private OpcaoPainel[] opcoesAtuais;

    private int ultimoSegundo = -1;

    /// <summary>Chamado quando o jogador usa um botão/atalho do painel (para tocar som).</summary>
    public Action aoInteragir;

    public bool PainelVisivel
    {
        get { return painel != null && painel.activeSelf; }
    }

    public bool BussolaLigada
    {
        get { return bussolaLigada; }
    }

    // ------------------------------------------------------------------
    // Construção
    // ------------------------------------------------------------------
    public void Construir(int[] itensPorFase)
    {
        GarantirEventSystem();

        spriteArredondado = Recursos.RetanguloArredondado();
        spriteCirculo = Recursos.Circulo();

        GameObject canvasGO = new GameObject("InterfaceCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler escala = canvasGO.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        Transform raiz = canvasGO.transform;

        // A ordem de criação é a ordem de desenho (o último fica por cima)
        ConstruirEscuridao(raiz);
        ConstruirHud(raiz, itensPorFase);
        ConstruirAvisos(raiz);
        ConstruirFade(raiz);
        ConstruirPainel(raiz);

        painel.SetActive(false);
        banner.SetActive(false);
        textoAviso.gameObject.SetActive(false);
        escuridao.gameObject.SetActive(false);
    }

    private void GarantirEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.transform.SetParent(transform, false);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void ConstruirEscuridao(Transform pai)
    {
        RectTransform rect = CriarRect("Escuridao", pai,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4096f, 4096f));

        escuridao = rect.gameObject.AddComponent<Image>();
        escuridao.sprite = Recursos.Vinheta();
        escuridao.raycastTarget = false;
        escuridaoRect = rect;
    }

    private void ConstruirHud(Transform pai, int[] itensPorFase)
    {
        // ---- Placa da fase (canto superior esquerdo) ----
        RectTransform placaFase = CriarPlaca("PlacaFase", pai,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(520f, 112f));

        textoFase = CriarTexto(placaFase, "TextoFase", "FASE 1 / 3", 44f,
            TextAlignmentOptions.Left, CorDestaque);
        textoFase.fontStyle = FontStyles.Bold;
        Posicionar(textoFase.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(28f, 0f), new Vector2(-16f, -8f));

        textoNomeFase = CriarTexto(placaFase, "TextoNomeFase", "", 28f,
            TextAlignmentOptions.Left, CorTexto);
        Posicionar(textoNomeFase.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(28f, 10f), new Vector2(-16f, 0f));

        // ---- Cronômetro (centro, em cima) ----
        RectTransform placaTempo = CriarPlaca("PlacaTempo", pai,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(220f, 76f));
        textoTempo = CriarTexto(placaTempo, "TextoTempo", "00:00", 46f,
            TextAlignmentOptions.Center, CorTexto);
        textoTempo.fontStyle = FontStyles.Bold;
        Esticar(textoTempo.rectTransform, 6f);

        // ---- Placa dos tesouros (canto superior direito) ----
        int total = 0;
        for (int g = 0; g < itensPorFase.Length; g++) total += itensPorFase[g];

        float larguraSlots = total * TamSlot + (total - 1) * EspSlot + (itensPorFase.Length - 1) * EspGrupo;
        float larguraPlaca = larguraSlots + 48f;

        RectTransform placaItens = CriarPlaca("PlacaTesouros", pai,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(larguraPlaca, 150f));

        TextMeshProUGUI titulo = CriarTexto(placaItens, "Titulo", "TESOUROS", 30f,
            TextAlignmentOptions.Left, CorTexto);
        titulo.fontStyle = FontStyles.Bold;
        Posicionar(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(0.4f, 1f),
            new Vector2(24f, -56f), new Vector2(0f, -12f));

        textoFaseItens = CriarTexto(placaItens, "TextoFaseItens", "Nesta fase: 0 / 3", 24f,
            TextAlignmentOptions.Center, new Color(CorTexto.r, CorTexto.g, CorTexto.b, 0.8f));
        Posicionar(textoFaseItens.rectTransform, new Vector2(0.3f, 1f), new Vector2(0.72f, 1f),
            new Vector2(0f, -56f), new Vector2(0f, -14f));

        textoContador = CriarTexto(placaItens, "Contador", "0 / 10", 40f,
            TextAlignmentOptions.Right, CorDestaque);
        textoContador.fontStyle = FontStyles.Bold;
        Posicionar(textoContador.rectTransform, new Vector2(0.6f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -60f), new Vector2(-24f, -8f));

        slotFundo = new Image[total];
        slotIcone = new Image[total];
        slotRect = new RectTransform[total];
        pulso = new float[total];

        float x = 24f;
        int indice = 0;
        for (int g = 0; g < itensPorFase.Length; g++)
        {
            for (int k = 0; k < itensPorFase[g]; k++)
            {
                RectTransform slot = CriarRect("Slot" + indice, placaItens,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 16f), new Vector2(TamSlot, TamSlot));
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(x + TamSlot * 0.5f, 16f + TamSlot * 0.5f);

                Image fundo = slot.gameObject.AddComponent<Image>();
                fundo.sprite = spriteArredondado;
                fundo.type = Image.Type.Sliced;
                fundo.raycastTarget = false;
                slotFundo[indice] = fundo;
                slotRect[indice] = slot;

                RectTransform icone = CriarRect("Icone", slot,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TamSlot - 8f, TamSlot - 8f));
                Image imgIcone = icone.gameObject.AddComponent<Image>();
                imgIcone.sprite = Recursos.Icone(indice);
                imgIcone.preserveAspect = true;
                imgIcone.raycastTarget = false;
                slotIcone[indice] = imgIcone;

                x += TamSlot + EspSlot;
                indice++;
            }
            x += EspGrupo;
        }

        // ---- Dica de controles (canto inferior esquerdo) ----
        RectTransform dica = CriarPlaca("PlacaDica", pai,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 28f), new Vector2(590f, 96f));
        TextMeshProUGUI textoDica = CriarTexto(dica, "TextoDica",
            "WASD / setas: mover  |  Shift: correr\nTab: bússola  |  M: som  |  Esc: pausa", 25f,
            TextAlignmentOptions.Center, new Color(CorTexto.r, CorTexto.g, CorTexto.b, 0.85f));
        Esticar(textoDica.rectTransform, 6f);

        // ---- Bússola (embaixo, no centro) ----
        RectTransform grupo = CriarRect("Bussola", pai,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(190f, 150f));
        bussola = grupo.gameObject;

        RectTransform disco = CriarRect("Disco", grupo,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(110f, 110f));
        Image imgDisco = disco.gameObject.AddComponent<Image>();
        imgDisco.sprite = spriteCirculo;
        imgDisco.color = CorPlaca;
        imgDisco.raycastTarget = false;

        setaBussola = CriarRect("Seta", disco,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
        Image imgSeta = setaBussola.gameObject.AddComponent<Image>();
        imgSeta.sprite = Recursos.Seta();
        imgSeta.color = CorDestaque;
        imgSeta.raycastTarget = false;

        textoDistancia = CriarTexto(grupo, "Distancia", "", 28f, TextAlignmentOptions.Center, CorTexto);
        textoDistancia.fontStyle = FontStyles.Bold;
        textoDistancia.outlineWidth = 0.25f;
        textoDistancia.outlineColor = new Color32(0, 0, 0, 255);
        Posicionar(textoDistancia.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 36f));
    }

    private void ConstruirAvisos(Transform pai)
    {
        // Aviso curto ("Moeda de Ouro coletada!")
        RectTransform rectAviso = CriarRect("Aviso", pai,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1200f, 80f));
        textoAviso = rectAviso.gameObject.AddComponent<TextMeshProUGUI>();
        textoAviso.fontSize = 48f;
        textoAviso.fontStyle = FontStyles.Bold;
        textoAviso.alignment = TextAlignmentOptions.Center;
        textoAviso.color = CorDestaque;
        textoAviso.raycastTarget = false;
        textoAviso.outlineWidth = 0.25f;
        textoAviso.outlineColor = new Color32(0, 0, 0, 255);

        // Faixa de início de fase
        RectTransform rectBanner = CriarRect("Banner", pai,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 220f));
        banner = rectBanner.gameObject;
        bannerGrupo = banner.AddComponent<CanvasGroup>();
        bannerGrupo.blocksRaycasts = false;
        bannerGrupo.interactable = false;

        bannerTitulo = CriarTexto(rectBanner, "Titulo", "FASE 1", 86f, TextAlignmentOptions.Center, CorDestaque);
        bannerTitulo.fontStyle = FontStyles.Bold;
        bannerTitulo.outlineWidth = 0.25f;
        bannerTitulo.outlineColor = new Color32(0, 0, 0, 255);
        Posicionar(bannerTitulo.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        bannerSub = CriarTexto(rectBanner, "Sub", "", 44f, TextAlignmentOptions.Center, CorTexto);
        bannerSub.outlineWidth = 0.25f;
        bannerSub.outlineColor = new Color32(0, 0, 0, 255);
        Posicionar(bannerSub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f), Vector2.zero, Vector2.zero);
    }

    private void ConstruirFade(Transform pai)
    {
        GameObject go = new GameObject("Fade", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        Esticar((RectTransform)go.transform, 0f);

        fade = go.AddComponent<Image>();
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = false;
    }

    private void ConstruirPainel(Transform pai)
    {
        // Fundo escurecido que cobre a tela toda
        GameObject fundo = new GameObject("Painel", typeof(RectTransform));
        fundo.transform.SetParent(pai, false);
        Esticar((RectTransform)fundo.transform, 0f);
        Image imgFundo = fundo.AddComponent<Image>();
        imgFundo.color = new Color(0f, 0f, 0f, 0.72f);
        painel = fundo;

        // Cartão central com borda dourada
        RectTransform borda = CriarRect("Borda", fundo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f));
        Image imgBorda = borda.gameObject.AddComponent<Image>();
        imgBorda.sprite = spriteArredondado;
        imgBorda.type = Image.Type.Sliced;
        imgBorda.color = CorBorda;

        RectTransform cartao = CriarRect("Cartao", borda,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(984f, 744f));
        Image imgCartao = cartao.gameObject.AddComponent<Image>();
        imgCartao.sprite = spriteArredondado;
        imgCartao.type = Image.Type.Sliced;
        imgCartao.color = new Color(0.14f, 0.10f, 0.08f, 1f);

        painelTitulo = CriarTexto(cartao, "Titulo", "TITULO", 84f, TextAlignmentOptions.Center, CorDestaque);
        painelTitulo.fontStyle = FontStyles.Bold;
        Posicionar(painelTitulo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -150f), new Vector2(-20f, -30f));

        painelCorpo = CriarTexto(cartao, "Corpo", "", 36f, TextAlignmentOptions.Top, CorTexto);
        Posicionar(painelCorpo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(50f, -405f), new Vector2(-50f, -165f));

        Color[] cores =
        {
            new Color(0.20f, 0.55f, 0.35f, 1f),
            new Color(0.55f, 0.40f, 0.22f, 1f),
            new Color(0.35f, 0.38f, 0.48f, 1f)
        };

        for (int i = 0; i < 3; i++)
        {
            int numero = i;
            botoes[i] = CriarBotao(cartao, "Botao" + (i + 1), cores[i], out rotulosBotoes[i]);
            botoes[i].onClick.AddListener(() => Disparar(numero));
        }
    }

    // ------------------------------------------------------------------
    // Atualização contínua (usa tempo real: funciona com o jogo pausado)
    // ------------------------------------------------------------------
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Aviso
        if (avisoRestante > 0f)
        {
            avisoRestante -= dt;
            if (avisoRestante <= 0f)
            {
                textoAviso.gameObject.SetActive(false);
            }
            else
            {
                float t = 1f - avisoRestante / avisoDuracao;
                Color c = textoAviso.color;
                c.a = Mathf.Clamp01(avisoRestante / 0.5f);
                textoAviso.color = c;
                textoAviso.rectTransform.anchoredPosition = new Vector2(0f, 230f + t * 40f);
            }
        }

        // Faixa de fase
        if (bannerRestante > 0f)
        {
            bannerRestante -= dt;
            if (bannerRestante <= 0f)
            {
                banner.SetActive(false);
            }
            else
            {
                float decorrido = bannerDuracao - bannerRestante;
                float entrada = Mathf.Clamp01(decorrido / 0.4f);
                float saida = Mathf.Clamp01(bannerRestante / 0.6f);
                bannerGrupo.alpha = Mathf.Min(entrada, saida);
            }
        }

        // Fade
        if (fadeVelocidade != 0f)
        {
            fadeAlfa = Mathf.Clamp01(fadeAlfa + fadeVelocidade * dt);
            fade.color = new Color(0f, 0f, 0f, fadeAlfa);
            if (fadeAlfa <= 0f || fadeAlfa >= 1f) fadeVelocidade = 0f;
        }

        // Pulso dos espaços de tesouro
        if (pulso != null)
        {
            for (int i = 0; i < pulso.Length; i++)
            {
                if (pulso[i] <= 0f) continue;

                pulso[i] -= dt;
                float k = Mathf.Clamp01(pulso[i] / DuracaoPulso);
                float escala = 1f + 0.5f * Mathf.Sin(k * Mathf.PI);
                slotRect[i].localScale = new Vector3(escala, escala, 1f);
            }
        }

        // Atalhos de teclado do painel
        if (PainelVisivel && opcoesAtuais != null)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space))
            {
                Disparar(0);
            }
            else
            {
                for (int i = 1; i < opcoesAtuais.Length; i++)
                {
                    if (Input.GetKeyDown(opcoesAtuais[i].atalho))
                    {
                        Disparar(i);
                        break;
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // API pública
    // ------------------------------------------------------------------
    public void AtualizarFase(int fase, int totalFases, string nome)
    {
        textoFase.text = "FASE " + fase + " / " + totalFases;
        textoNomeFase.text = nome;
    }

    public void AtualizarContagem(int coletadosTotal, int totalItens, int coletadosFase, int itensDaFase)
    {
        textoContador.text = coletadosTotal + " / " + totalItens;
        textoFaseItens.text = "Nesta fase: " + coletadosFase + " / " + itensDaFase;
    }

    /// <summary>Mostra quais tesouros já foram pegos; destaca os da fase atual.</summary>
    public void AtualizarSlots(bool[] coletados, int inicioFase, int fimFase)
    {
        for (int i = 0; i < slotFundo.Length; i++)
        {
            bool naFase = i >= inicioFase && i < fimFase;

            if (coletados[i])
            {
                slotFundo[i].color = new Color(0.62f, 0.44f, 0.12f, 1f);
                slotIcone[i].color = Color.white;
            }
            else
            {
                slotFundo[i].color = naFase
                    ? new Color(0.34f, 0.25f, 0.17f, 1f)
                    : new Color(0.20f, 0.15f, 0.12f, 0.9f);
                slotIcone[i].color = new Color(0f, 0f, 0f, naFase ? 0.65f : 0.4f);
            }
        }
    }

    public void PulsarSlot(int indice)
    {
        if (pulso != null && indice >= 0 && indice < pulso.Length) pulso[indice] = DuracaoPulso;
    }

    public void AtualizarTempo(float segundos)
    {
        int s = (int)segundos;
        if (s == ultimoSegundo) return;

        ultimoSegundo = s;
        textoTempo.text = Recursos.FormatarTempo(segundos, false);
    }

    public void DefinirBussola(bool visivel, float anguloGraus, float distancia)
    {
        bool mostrar = visivel && bussolaLigada;
        if (bussola.activeSelf != mostrar) bussola.SetActive(mostrar);
        if (!mostrar) return;

        // A seta desenhada aponta para cima; 0 grau = direita, então tira 90
        setaBussola.localRotation = Quaternion.Euler(0f, 0f, anguloGraus - 90f);
        textoDistancia.text = Mathf.RoundToInt(distancia) + " m";
    }

    public bool AlternarBussola()
    {
        bussolaLigada = !bussolaLigada;
        return bussolaLigada;
    }

    public void MostrarAviso(string texto)
    {
        textoAviso.text = texto;
        textoAviso.color = CorDestaque;
        textoAviso.rectTransform.anchoredPosition = new Vector2(0f, 230f);
        textoAviso.gameObject.SetActive(true);
        avisoRestante = avisoDuracao;
    }

    public void MostrarBanner(string titulo, string subtitulo)
    {
        bannerTitulo.text = titulo;
        bannerSub.text = subtitulo;
        bannerGrupo.alpha = 0f;
        banner.SetActive(true);
        bannerRestante = bannerDuracao;
    }

    /// <summary>Começa com a tela preta e clareia aos poucos.</summary>
    public void ClarearTela(float duracao)
    {
        fadeAlfa = 1f;
        fade.color = new Color(0f, 0f, 0f, 1f);
        fadeVelocidade = -1f / Mathf.Max(0.05f, duracao);
    }

    public void DefinirEscuridao(bool ligada)
    {
        escuridao.gameObject.SetActive(ligada);
    }

    /// <summary>Mantém o "buraco de luz" da fase 3 em cima do jogador.</summary>
    public void PosicionarEscuridao(Camera camera, Vector3 posicaoMundo)
    {
        if (camera == null || !escuridao.gameObject.activeSelf) return;

        Vector3 tela = camera.WorldToScreenPoint(posicaoMundo);
        Vector2 centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float fator = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        escuridaoRect.anchoredPosition = (new Vector2(tela.x, tela.y) - centro) / fator;
    }

    public void MostrarPainel(string titulo, string corpo, params OpcaoPainel[] opcoes)
    {
        painelTitulo.text = titulo;
        painelCorpo.text = corpo;
        opcoesAtuais = opcoes;

        int n = Mathf.Min(opcoes.Length, botoes.Length);
        for (int i = 0; i < botoes.Length; i++)
        {
            bool ativo = i < n;
            botoes[i].gameObject.SetActive(ativo);
            if (!ativo) continue;

            rotulosBotoes[i].text = opcoes[i].rotulo;

            // O primeiro botão fica em cima; a pilha começa embaixo do cartão
            RectTransform rect = (RectTransform)botoes[i].transform;
            rect.anchoredPosition = new Vector2(0f, 40f + (n - 1 - i) * 96f);
        }

        painel.SetActive(true);
    }

    public void EsconderPainel()
    {
        if (painel != null) painel.SetActive(false);
        opcoesAtuais = null;
    }

    private void Disparar(int numero)
    {
        if (opcoesAtuais == null || numero >= opcoesAtuais.Length) return;

        // Evita que Enter/Espaço "cliquem" de novo no botão que ficou selecionado
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Action acao = opcoesAtuais[numero].acao;

        if (aoInteragir != null) aoInteragir();
        if (acao != null) acao();
    }

    // ------------------------------------------------------------------
    // Utilitários de UI
    // ------------------------------------------------------------------
    private RectTransform CriarPlaca(string nome, Transform pai, Vector2 ancora, Vector2 pivo,
                                     Vector2 posicao, Vector2 tamanho)
    {
        RectTransform rect = CriarRect(nome, pai, ancora, pivo, posicao, tamanho);
        Image imagem = rect.gameObject.AddComponent<Image>();
        imagem.sprite = spriteArredondado;
        imagem.type = Image.Type.Sliced;
        imagem.color = CorPlaca;
        imagem.raycastTarget = false;
        return rect;
    }

    private static RectTransform CriarRect(string nome, Transform pai, Vector2 ancora,
                                           Vector2 pivo, Vector2 posicao, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(pai, false);
        rect.anchorMin = ancora;
        rect.anchorMax = ancora;
        rect.pivot = pivo;
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        return rect;
    }

    private static void Esticar(RectTransform rect, float margem)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(margem, margem);
        rect.offsetMax = new Vector2(-margem, -margem);
    }

    // Define âncoras e margens (offsetMin = esquerda/baixo, offsetMax = direita/cima)
    private static void Posicionar(RectTransform rect, Vector2 ancoraMin, Vector2 ancoraMax,
                                   Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = ancoraMin;
        rect.anchorMax = ancoraMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static TextMeshProUGUI CriarTexto(Transform pai, string nome, string conteudo,
                                              float tamanho, TextAlignmentOptions alinhamento,
                                              Color cor)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.alignment = alinhamento;
        texto.color = cor;
        texto.raycastTarget = false;
        return texto;
    }

    private Button CriarBotao(Transform pai, string nome, Color cor, out TextMeshProUGUI rotulo)
    {
        RectTransform rect = CriarRect(nome, pai,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(620f, 82f));

        Image imagem = rect.gameObject.AddComponent<Image>();
        imagem.sprite = spriteArredondado;
        imagem.type = Image.Type.Sliced;
        imagem.color = cor;

        Button botao = rect.gameObject.AddComponent<Button>();
        botao.targetGraphic = imagem;

        ColorBlock cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cores.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        botao.colors = cores;

        rotulo = CriarTexto(rect, "Rotulo", "", 38f, TextAlignmentOptions.Center, Color.white);
        rotulo.fontStyle = FontStyles.Bold;
        Esticar(rotulo.rectTransform, 8f);

        return botao;
    }
}
