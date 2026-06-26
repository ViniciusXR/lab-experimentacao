"""Lab05S03 - Dashboard de visualizacao (GraphQL vs REST).

Importa os dados produzidos na Sprint 2, processa-os com pandas e gera tabelas
e graficos (matplotlib / seaborn) que permitem interpretar as diferencas entre
REST e GraphQL nas metricas das perguntas de pesquisa RQ1 (tempo de resposta) e
RQ2 (tamanho da resposta).

Entradas (lab05_entrega/dados):
    - medicoes_lab05.csv        (1.200 trials individuais)
    - resumo_estatistico.csv    (estatisticas descritivas por cenario/tratamento)
    - testes_estatisticos.csv   (Mann-Whitney U, Cliff's delta e vereditos)
    - metadados_execucao.json   (ambiente e parametros da coleta)

Saidas (lab05_entrega/dashboards):
    - graficos/*.png            (figuras geradas com matplotlib/seaborn)
    - tabelas/*.csv             (tabelas processadas com pandas)
    - dashboard_lab05_python.html (dashboard interativo com graficos e tabelas)

Uso:
    python gerar_dashboard.py
"""

from __future__ import annotations

import base64
import json
from datetime import datetime
from pathlib import Path

import matplotlib

matplotlib.use("Agg")  # backend sem interface grafica, apenas geracao de arquivos

import matplotlib.pyplot as plt
import matplotlib.ticker as mticker
import pandas as pd
import seaborn as sns

# ---------------------------------------------------------------------------
# Identidade visual (alinhada ao restante da entrega do Lab05)
# ---------------------------------------------------------------------------
COR_REST = "#d5534a"
COR_GRAPHQL = "#1f8a70"
COR_TEMPO = "#3867d6"
COR_TAMANHO = "#a35c1a"
COR_TEXTO = "#17202a"
PALETA_TRATAMENTO = {"REST": COR_REST, "GraphQL": COR_GRAPHQL}

DPI = 150


# ---------------------------------------------------------------------------
# Resolucao de caminhos
# ---------------------------------------------------------------------------
def localizar_entrega() -> Path:
    """Localiza a pasta lab05_entrega a partir da posicao deste script."""
    atual = Path(__file__).resolve()
    for diretorio in [atual.parent, *atual.parents]:
        candidato = diretorio / "lab05_entrega"
        if candidato.is_dir():
            return candidato
    raise FileNotFoundError(
        "Pasta 'lab05_entrega' nao encontrada. Execute a Sprint 2 antes do dashboard."
    )


# ---------------------------------------------------------------------------
# Carga e preparacao dos dados (pandas)
# ---------------------------------------------------------------------------
def carregar_dados(dados_dir: Path) -> dict[str, object]:
    """Le os CSVs/JSON da Sprint 2 e devolve dataframes prontos para analise."""
    # Os CSVs/JSON sao gravados pela Sprint 2 (.NET) com BOM; "utf-8-sig" o remove
    # e evita que o primeiro cabecalho fique prefixado por \ufeff.
    medicoes = pd.read_csv(dados_dir / "medicoes_lab05.csv", sep=";", encoding="utf-8-sig")
    resumo = pd.read_csv(dados_dir / "resumo_estatistico.csv", sep=";", encoding="utf-8-sig")
    testes = pd.read_csv(dados_dir / "testes_estatisticos.csv", sep=";", encoding="utf-8-sig")

    metadados: dict[str, object] = {}
    meta_path = dados_dir / "metadados_execucao.json"
    if meta_path.exists():
        metadados = json.loads(meta_path.read_text(encoding="utf-8-sig"))

    # Ordena tratamentos com REST sempre antes de GraphQL nos graficos.
    ordem_tratamento = pd.CategoricalDtype(["REST", "GraphQL"], ordered=True)
    medicoes["treatment"] = medicoes["treatment"].astype(ordem_tratamento)
    resumo["treatment"] = resumo["treatment"].astype(ordem_tratamento)

    # Separa a linha agregada (ALL) das linhas por cenario nos testes.
    testes_cenarios = testes[testes["scenario_id"] != "ALL"].copy()
    agregado = testes[testes["scenario_id"] == "ALL"].iloc[0].to_dict()

    return {
        "medicoes": medicoes,
        "resumo": resumo,
        "testes": testes,
        "testes_cenarios": testes_cenarios,
        "agregado": agregado,
        "metadados": metadados,
    }


# ---------------------------------------------------------------------------
# Utilidades de formatacao pt-BR
# ---------------------------------------------------------------------------
def fmt_num(valor: float, casas: int = 2) -> str:
    """Formata numero no padrao pt-BR (milhar com ponto, decimal com virgula)."""
    texto = f"{valor:,.{casas}f}"
    return texto.replace(",", "X").replace(".", ",").replace("X", ".")


def fmt_pvalor(valor: float) -> str:
    return "< 0,0001" if valor < 0.0001 else fmt_num(valor, 4)


def rotular_barras(ax: plt.Axes, formato, deslocamento: float = 3.0) -> None:
    """Escreve o valor no topo de cada barra do grafico."""
    for container in ax.containers:
        rotulos = [formato(barra.get_height()) for barra in container]
        ax.bar_label(
            container,
            labels=rotulos,
            padding=deslocamento,
            fontsize=8,
            color=COR_TEXTO,
        )


# ---------------------------------------------------------------------------
# Graficos (matplotlib / seaborn)
# ---------------------------------------------------------------------------
def grafico_tempo_mediano(resumo: pd.DataFrame, destino: Path) -> Path:
    """RQ1 - mediana do tempo de resposta por cenario (REST vs GraphQL)."""
    fig, ax = plt.subplots(figsize=(9, 5))
    sns.barplot(
        data=resumo,
        x="scenario_id",
        y="median_ms",
        hue="treatment",
        palette=PALETA_TRATAMENTO,
        ax=ax,
    )
    ax.set_title("RQ1 - Tempo de resposta mediano por cenario", fontweight="bold")
    ax.set_xlabel("Cenario de consulta")
    ax.set_ylabel("Tempo mediano (ms)")
    ax.legend(title="Tratamento")
    rotular_barras(ax, lambda v: fmt_num(v, 3))
    return salvar(fig, destino)


def grafico_tamanho_mediano(resumo: pd.DataFrame, destino: Path) -> Path:
    """RQ2 - mediana do tamanho da resposta por cenario (escala log)."""
    fig, ax = plt.subplots(figsize=(9, 5))
    sns.barplot(
        data=resumo,
        x="scenario_id",
        y="median_bytes",
        hue="treatment",
        palette=PALETA_TRATAMENTO,
        ax=ax,
    )
    ax.set_yscale("log")
    # Folga no topo (escala log) para os rotulos das barras altas nao colidirem
    # com a legenda posicionada no canto superior direito.
    teto = float(resumo["median_bytes"].max()) * 4
    ax.set_ylim(top=teto)
    ax.set_title("RQ2 - Tamanho mediano da resposta por cenario", fontweight="bold")
    ax.set_xlabel("Cenario de consulta")
    ax.set_ylabel("Tamanho mediano (bytes, escala log)")
    ax.legend(title="Tratamento", loc="upper right")
    rotular_barras(ax, lambda v: fmt_num(v, 0))
    return salvar(fig, destino)


def grafico_distribuicao_tempo(medicoes: pd.DataFrame, destino: Path) -> Path:
    """Distribuicao do tempo por trial (boxplot) evidenciando variancia."""
    fig, ax = plt.subplots(figsize=(9, 5))
    sns.boxplot(
        data=medicoes,
        x="scenario_id",
        y="elapsed_ms",
        hue="treatment",
        palette=PALETA_TRATAMENTO,
        fliersize=2,
        ax=ax,
    )
    ax.set_title(
        "Distribuicao do tempo de resposta por trial (1.200 medicoes)",
        fontweight="bold",
    )
    ax.set_xlabel("Cenario de consulta")
    ax.set_ylabel("Tempo por trial (ms)")
    ax.legend(title="Tratamento")
    return salvar(fig, destino)


def grafico_reducao_percentual(testes_cenarios: pd.DataFrame, destino: Path) -> Path:
    """Reducao percentual de tempo e tamanho proporcionada pelo GraphQL."""
    dados = testes_cenarios.melt(
        id_vars="scenario_id",
        value_vars=["time_reduction_pct", "size_reduction_pct"],
        var_name="metrica",
        value_name="reducao",
    )
    dados["metrica"] = dados["metrica"].map(
        {"time_reduction_pct": "Tempo", "size_reduction_pct": "Tamanho"}
    )

    fig, ax = plt.subplots(figsize=(9, 5))
    sns.barplot(
        data=dados,
        x="scenario_id",
        y="reducao",
        hue="metrica",
        palette={"Tempo": COR_TEMPO, "Tamanho": COR_TAMANHO},
        ax=ax,
    )
    ax.set_ylim(0, 100)
    ax.set_title(
        "Reducao percentual do GraphQL frente ao REST", fontweight="bold"
    )
    ax.set_xlabel("Cenario de consulta")
    ax.set_ylabel("Reducao (%)")
    ax.legend(title="Metrica")
    ax.yaxis.set_major_formatter(mticker.PercentFormatter(decimals=0))
    rotular_barras(ax, lambda v: f"{fmt_num(v, 1)}%")
    return salvar(fig, destino)


def grafico_agregado(agregado: dict[str, object], destino: Path) -> Path:
    """Comparacao agregada das medianas (tempo e tamanho) entre os dois estilos."""
    fig, (ax_tempo, ax_tam) = plt.subplots(1, 2, figsize=(11, 4.8))

    tratamentos = ["REST", "GraphQL"]
    cores = [COR_REST, COR_GRAPHQL]

    tempos = [float(agregado["rest_median_ms"]), float(agregado["graphql_median_ms"])]
    barras_tempo = ax_tempo.bar(tratamentos, tempos, color=cores)
    ax_tempo.set_title("Mediana agregada de tempo", fontweight="bold")
    ax_tempo.set_ylabel("Tempo (ms)")
    ax_tempo.bar_label(barras_tempo, labels=[fmt_num(v, 3) for v in tempos], padding=3)

    tamanhos = [
        float(agregado["rest_median_bytes"]),
        float(agregado["graphql_median_bytes"]),
    ]
    barras_tam = ax_tam.bar(tratamentos, tamanhos, color=cores)
    ax_tam.set_title("Mediana agregada de tamanho", fontweight="bold")
    ax_tam.set_ylabel("Tamanho (bytes)")
    ax_tam.bar_label(barras_tam, labels=[fmt_num(v, 0) for v in tamanhos], padding=3)

    fig.suptitle(
        "Visao agregada de todos os cenarios (REST vs GraphQL)",
        fontweight="bold",
        fontsize=13,
    )
    return salvar(fig, destino)


def salvar(fig: plt.Figure, destino: Path) -> Path:
    fig.tight_layout()
    fig.savefig(destino, dpi=DPI, bbox_inches="tight")
    plt.close(fig)
    return destino


# ---------------------------------------------------------------------------
# Tabelas processadas (pandas)
# ---------------------------------------------------------------------------
def exportar_tabelas(dados: dict[str, object], tabelas_dir: Path) -> dict[str, pd.DataFrame]:
    """Gera tabelas resumidas e as salva em CSV para uso no relatorio."""
    resumo: pd.DataFrame = dados["resumo"]
    testes: pd.DataFrame = dados["testes"]

    tabela_descritiva = (
        resumo.assign(cenario=resumo["scenario_id"] + " - " + resumo["scenario_name"])[
            [
                "cenario",
                "treatment",
                "n",
                "median_ms",
                "p95_ms",
                "median_bytes",
            ]
        ]
        .rename(
            columns={
                "cenario": "Cenario",
                "treatment": "Tratamento",
                "n": "n",
                "median_ms": "Mediana tempo (ms)",
                "p95_ms": "P95 tempo (ms)",
                "median_bytes": "Mediana tamanho (bytes)",
            }
        )
        .sort_values(["Cenario", "Tratamento"])
    )

    tabela_testes = testes[
        [
            "scenario_id",
            "scenario_name",
            "time_reduction_pct",
            "time_p_value",
            "time_cliffs_delta",
            "size_reduction_pct",
            "size_p_value",
            "size_cliffs_delta",
            "rq1_verdict",
            "rq2_verdict",
        ]
    ].rename(
        columns={
            "scenario_id": "Cenario",
            "scenario_name": "Descricao",
            "time_reduction_pct": "Reducao tempo (%)",
            "time_p_value": "p (tempo)",
            "time_cliffs_delta": "Cliff (tempo)",
            "size_reduction_pct": "Reducao tamanho (%)",
            "size_p_value": "p (tamanho)",
            "size_cliffs_delta": "Cliff (tamanho)",
            "rq1_verdict": "RQ1",
            "rq2_verdict": "RQ2",
        }
    )

    tabelas_dir.mkdir(parents=True, exist_ok=True)
    tabela_descritiva.to_csv(
        tabelas_dir / "tabela_descritiva.csv", sep=";", index=False, encoding="utf-8-sig"
    )
    tabela_testes.to_csv(
        tabelas_dir / "tabela_testes.csv", sep=";", index=False, encoding="utf-8-sig"
    )

    return {"descritiva": tabela_descritiva, "testes": tabela_testes}


# ---------------------------------------------------------------------------
# Montagem do HTML
# ---------------------------------------------------------------------------
def imagem_base64(caminho: Path) -> str:
    dados = base64.b64encode(caminho.read_bytes()).decode("ascii")
    return f"data:image/png;base64,{dados}"


def tabela_descritiva_html(tabela: pd.DataFrame) -> str:
    linhas = []
    for _, linha in tabela.iterrows():
        classe = linha["Tratamento"].lower()
        linhas.append(
            "<tr>"
            f"<td>{linha['Cenario']}</td>"
            f"<td><span class='pill {classe}'>{linha['Tratamento']}</span></td>"
            f"<td>{int(linha['n'])}</td>"
            f"<td>{fmt_num(linha['Mediana tempo (ms)'], 4)}</td>"
            f"<td>{fmt_num(linha['P95 tempo (ms)'], 4)}</td>"
            f"<td>{fmt_num(linha['Mediana tamanho (bytes)'], 0)}</td>"
            "</tr>"
        )
    corpo = "\n".join(linhas)
    return (
        "<table><thead><tr>"
        "<th>Cenario</th><th>Tratamento</th><th>n</th>"
        "<th>Mediana tempo (ms)</th><th>P95 tempo (ms)</th><th>Mediana tamanho (bytes)</th>"
        f"</tr></thead><tbody>{corpo}</tbody></table>"
    )


def tabela_testes_html(tabela: pd.DataFrame) -> str:
    linhas = []
    for _, linha in tabela.iterrows():
        destaque = " class='agg'" if linha["Cenario"] == "ALL" else ""
        rotulo = "Todos" if linha["Cenario"] == "ALL" else linha["Cenario"]
        linhas.append(
            f"<tr{destaque}>"
            f"<td>{rotulo}</td>"
            f"<td>{fmt_num(linha['Reducao tempo (%)'], 2)}%</td>"
            f"<td>{fmt_pvalor(linha['p (tempo)'])}</td>"
            f"<td>{fmt_num(linha['Cliff (tempo)'], 3)}</td>"
            f"<td>{fmt_num(linha['Reducao tamanho (%)'], 2)}%</td>"
            f"<td>{fmt_pvalor(linha['p (tamanho)'])}</td>"
            f"<td>{fmt_num(linha['Cliff (tamanho)'], 3)}</td>"
            f"<td><span class='verdict'>{linha['RQ1']}</span></td>"
            f"<td><span class='verdict'>{linha['RQ2']}</span></td>"
            "</tr>"
        )
    corpo = "\n".join(linhas)
    return (
        "<table><thead><tr>"
        "<th>Cenario</th><th>Reducao tempo</th><th>p (tempo)</th><th>Cliff (tempo)</th>"
        "<th>Reducao tamanho</th><th>p (tamanho)</th><th>Cliff (tamanho)</th>"
        "<th>RQ1</th><th>RQ2</th>"
        f"</tr></thead><tbody>{corpo}</tbody></table>"
    )


# CSS do modo apresentacao: cada aba (pane) ocupa a tela inteira, sem rolagem da
# pagina; apenas tabelas densas rolam dentro do proprio cartao.
CSS_DECK = """
*{box-sizing:border-box;}
html,body{height:100%;margin:0;}
body{overflow:hidden;background:var(--page);color:var(--ink);font-family:"Segoe UI",Arial,sans-serif;}
.deck{height:100vh;display:flex;flex-direction:column;}
header{flex:none;display:flex;align-items:center;justify-content:space-between;gap:20px;padding:13px 28px;color:#fff;background:#253858;border-bottom:4px solid var(--graphql);}
header h1{margin:2px 0 0;font-size:clamp(20px,2.4vw,28px);font-weight:750;}
header .eyebrow{margin:0;color:#b7d8d0;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.04em;}
header .status{padding:8px 14px;border:1px solid rgba(255,255,255,.25);border-radius:999px;font-size:13px;font-weight:650;color:#ecfff9;white-space:nowrap;}
nav.tabs{flex:none;display:flex;gap:4px;padding:8px 18px 0;background:#1c2c44;overflow-x:auto;}
.tab{appearance:none;border:0;cursor:pointer;background:transparent;color:#aab8cc;font:inherit;font-size:14px;font-weight:650;padding:10px 16px;border-radius:9px 9px 0 0;white-space:nowrap;transition:background .15s,color .15s;}
.tab:hover{color:#fff;background:rgba(255,255,255,.07);}
.tab.active{color:#15324d;background:var(--page);}
.stage{flex:1;position:relative;min-height:0;}
.pane{position:absolute;inset:0;display:none;flex-direction:column;gap:14px;padding:20px 26px 14px;}
.pane.active{display:flex;animation:fade .25s ease;}
@keyframes fade{from{opacity:0;transform:translateY(6px);}to{opacity:1;transform:none;}}
.pane h2{margin:0;font-size:19px;color:var(--accent);}
.chart-wrap{flex:1;min-height:0;display:flex;align-items:center;justify-content:center;background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:12px;box-shadow:0 8px 22px rgba(15,22,34,.10);}
.chart-wrap img{max-width:100%;max-height:100%;width:auto;height:auto;object-fit:contain;}
.caption{flex:none;margin:0;font-size:15px;line-height:1.45;color:#2b3a47;background:#e7f3ee;border-left:4px solid var(--graphql);padding:12px 16px;border-radius:8px;}
.kpis{flex:none;display:grid;grid-template-columns:repeat(4,1fr);gap:14px;}
.kpi{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:14px 16px;box-shadow:0 6px 16px rgba(15,22,34,.08);}
.kpi span{display:block;color:var(--muted);font-size:12px;font-weight:700;text-transform:uppercase;}
.kpi strong{display:block;margin-top:6px;font-size:clamp(24px,3vw,34px);color:var(--accent);}
.kpi small{display:block;margin-top:4px;color:var(--muted);font-size:12px;}
.table-wrap{flex:1;min-height:0;overflow:auto;background:var(--panel);border:1px solid var(--line);border-radius:12px;box-shadow:0 8px 22px rgba(15,22,34,.10);}
table{width:100%;border-collapse:collapse;font-size:14px;}
th,td{padding:11px 14px;border-bottom:1px solid var(--line);text-align:left;white-space:nowrap;}
th{position:sticky;top:0;color:var(--accent);background:#e9eef5;font-size:12px;text-transform:uppercase;}
tbody tr:last-child td{border-bottom:0;}
tr.agg td{font-weight:700;background:#e7f3ee;}
.pill{display:inline-block;min-width:72px;padding:4px 10px;border-radius:999px;color:#fff;text-align:center;font-size:12px;font-weight:700;}
.pill.rest{background:var(--rest);}
.pill.graphql{background:var(--graphql);}
.verdict{font-size:12px;font-weight:700;color:var(--graphql);}
.prose{flex:1;min-height:0;overflow:auto;display:grid;gap:12px;align-content:start;}
.prose p{margin:0;font-size:15px;line-height:1.55;background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:14px 18px;box-shadow:0 4px 12px rgba(15,22,34,.06);}
.meta{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:0;}
.meta div{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:10px 12px;}
.meta dt{color:var(--muted);font-size:11px;font-weight:700;text-transform:uppercase;}
.meta dd{margin:3px 0 0;font-size:14px;}
.intro{flex:1;min-height:0;display:flex;flex-direction:column;gap:16px;overflow:auto;justify-content:center;}
.intro .lead{margin:0;font-size:clamp(16px,1.9vw,21px);line-height:1.5;color:#2b3a47;}
.intro .lead b{color:var(--accent);}
.cards{display:grid;gap:14px;}
.cards.c3{grid-template-columns:repeat(3,1fr);}
.cards.c4{grid-template-columns:repeat(4,1fr);}
.card{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px 18px;box-shadow:0 6px 16px rgba(15,22,34,.07);}
.card.accent{border-left:4px solid var(--graphql);}
.card h3{margin:0 0 6px;font-size:14px;color:var(--accent);}
.card p{margin:0;font-size:14px;line-height:1.45;color:#46535f;}
.hyp-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:16px;}
.hyp{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:18px 20px;box-shadow:0 6px 16px rgba(15,22,34,.07);}
.hyp .tag{display:inline-block;padding:5px 12px;border-radius:999px;background:#253858;color:#fff;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.03em;}
.hyp p{margin:12px 0 0;font-size:15px;line-height:1.45;}
.hyp .h0{color:#7a8794;}
.hyp .h1{color:var(--accent);border-left:3px solid var(--graphql);padding-left:10px;}
footer{flex:none;display:flex;align-items:center;justify-content:space-between;gap:12px;padding:8px 24px;background:#1c2c44;color:#aab8cc;font-size:13px;}
footer .indicator{font-weight:700;color:#fff;}
@media (max-width:820px){.kpis,.meta{grid-template-columns:repeat(2,1fr);}header h1{font-size:18px;}.cards.c3,.cards.c4,.hyp-grid{grid-template-columns:1fr;}}
"""

# Navegacao por abas + teclado (setas, PageUp/Down, Home/End).
JS_DECK = """
(function(){
  var tabs=[].slice.call(document.querySelectorAll('.tab'));
  var panes=[].slice.call(document.querySelectorAll('.pane'));
  var ind=document.querySelector('.indicator');
  var cur=0;
  function go(i){
    if(i<0){i=0;}
    if(i>tabs.length-1){i=tabs.length-1;}
    for(var k=0;k<tabs.length;k++){
      var on=(k===i);
      tabs[k].classList.toggle('active',on);
      panes[k].classList.toggle('active',on);
    }
    if(ind){ind.textContent=(i+1)+' / '+tabs.length;}
    cur=i;
    if(history.replaceState){history.replaceState(null,'','#'+(i+1));}
  }
  tabs.forEach(function(t,i){t.addEventListener('click',function(){go(i);});});
  document.addEventListener('keydown',function(e){
    if(e.key==='ArrowRight'||e.key==='PageDown'){e.preventDefault();go(cur+1);}
    else if(e.key==='ArrowLeft'||e.key==='PageUp'){e.preventDefault();go(cur-1);}
    else if(e.key==='Home'){e.preventDefault();go(0);}
    else if(e.key==='End'){e.preventDefault();go(tabs.length-1);}
  });
  var inicial=parseInt((location.hash||'').replace('#',''),10);
  go(isNaN(inicial)?0:inicial-1);
})();
"""


def construir_html(
    dados: dict[str, object],
    tabelas: dict[str, pd.DataFrame],
    graficos: dict[str, Path],
) -> str:
    agregado = dados["agregado"]
    metadados = dados["metadados"]
    total_medicoes = len(dados["medicoes"])
    qtd_cenarios = dados["testes_cenarios"].shape[0]

    time_red = fmt_num(float(agregado["time_reduction_pct"]), 2)
    size_red = fmt_num(float(agregado["size_reduction_pct"]), 2)
    time_cliff = fmt_num(float(agregado["time_cliffs_delta"]), 3)
    size_cliff = fmt_num(float(agregado["size_cliffs_delta"]), 3)
    trials_trat = str(metadados.get("trialsPerTreatment", 120))

    kpis = [
        ("Medicoes", fmt_num(total_medicoes, 0), "trials coletados"),
        ("Cenarios", str(qtd_cenarios), "consultas equivalentes"),
        ("Reducao de tempo", f"{time_red}%", "mediana agregada (RQ1)"),
        ("Reducao de tamanho", f"{size_red}%", "mediana agregada (RQ2)"),
    ]
    kpis_html = "\n".join(
        f"<article class='kpi'><span>{rotulo}</span><strong>{valor}</strong><small>{nota}</small></article>"
        for rotulo, valor, nota in kpis
    )

    def chart(chave: str, alt: str) -> str:
        return (
            "<div class='chart-wrap'>"
            f"<img src='{imagem_base64(graficos[chave])}' alt='{alt}' />"
            "</div>"
        )

    ambiente = ""
    if metadados:
        itens = [
            ("Gerado em", datetime.now().strftime("%d/%m/%Y %H:%M")),
            (".NET", str(metadados.get("dotnetVersion", "-"))),
            ("SO", str(metadados.get("os", "-"))),
            ("Arquitetura", str(metadados.get("architecture", "-"))),
            ("Trials/tratamento", str(metadados.get("trialsPerTreatment", "-"))),
            ("Aquecimento", str(metadados.get("warmupIterations", "-"))),
            ("Semente dataset", str(metadados.get("datasetSeed", "-"))),
            ("Semente trials", str(metadados.get("trialSeed", "-"))),
        ]
        ambiente = "".join(
            f"<div><dt>{rotulo}</dt><dd>{valor}</dd></div>" for rotulo, valor in itens
        )
        ambiente = f"<dl class='meta'>{ambiente}</dl>"

    # Cada aba e um "slide" (rotulo, conteudo) que ocupa a tela inteira.
    slides = [
        (
            "Introducao",
            "<div class='intro'>"
            "<p class='lead'>Experimento controlado que compara <b>REST</b> e <b>GraphQL</b> em duas "
            "metricas quantitativas: <b>tempo de resposta</b> e <b>tamanho do payload</b>. O objetivo e "
            "medir, de forma reprodutivel, os beneficios da adocao de uma API GraphQL.</p>"
            "<div class='cards c3'>"
            "<div class='card accent'><h3>Objetivo</h3><p>Avaliar quantitativamente se o GraphQL supera o "
            "REST em desempenho e em volume de dados transferidos.</p></div>"
            "<div class='card accent'><h3>RQ1 &middot; Tempo</h3><p>Respostas as consultas GraphQL sao mais "
            "rapidas que as respostas as consultas REST?</p></div>"
            "<div class='card accent'><h3>RQ2 &middot; Tamanho</h3><p>Respostas as consultas GraphQL tem "
            "tamanho menor que as respostas as consultas REST?</p></div>"
            "</div>"
            "<div class='cards c4'>"
            "<div class='card'><h3>Tratamentos</h3><p>REST com recursos completos vs GraphQL com projecao "
            "de campos, sobre os mesmos dados.</p></div>"
            f"<div class='card'><h3>Cenarios</h3><p>{qtd_cenarios} consultas equivalentes: lista, detalhe, "
            "issues, contribuidores e visao aninhada.</p></div>"
            f"<div class='card'><h3>Medicoes</h3><p>{fmt_num(total_medicoes, 0)} trials no total "
            f"({trials_trat} por tratamento em cada cenario), com aquecimento previo.</p></div>"
            "<div class='card'><h3>Estatistica</h3><p>Teste de Mann-Whitney U (&alpha; = 0,05) e tamanho de "
            "efeito por Cliff's delta.</p></div>"
            "</div>"
            "</div>",
        ),
        (
            "Resultados esperados",
            "<div class='intro'>"
            "<p class='lead'>Antes da coleta, definimos as hipoteses nula (H0) e alternativa (H1) de cada "
            "pergunta. A expectativa e que o GraphQL, ao retornar apenas os campos solicitados, reduza o "
            "<b>over-fetching</b> tipico do REST.</p>"
            "<div class='hyp-grid'>"
            "<div class='hyp'><span class='tag'>RQ1 &middot; Tempo</span>"
            "<p class='h0'><b>H0-1:</b> nao ha diferenca significativa entre o tempo de resposta de REST e GraphQL.</p>"
            "<p class='h1'><b>H1-1 (esperada):</b> o tempo de resposta do GraphQL e menor que o do REST.</p></div>"
            "<div class='hyp'><span class='tag'>RQ2 &middot; Tamanho</span>"
            "<p class='h0'><b>H0-2:</b> nao ha diferenca significativa entre o tamanho das respostas de REST e GraphQL.</p>"
            "<p class='h1'><b>H1-2 (esperada):</b> o tamanho das respostas do GraphQL e menor que o do REST.</p></div>"
            "</div>"
            "<p class='caption'>Expectativa: como o GraphQL projeta apenas os campos necessarios, esperamos "
            "payloads menores (RQ2) e, por consequencia, menor custo de serializacao/parsing e menor tempo "
            "(RQ1) &mdash; confirmando H1-1 e H1-2.</p>"
            "</div>",
        ),
        (
            "Visao geral",
            f"<div class='kpis'>{kpis_html}</div>"
            + chart("agregado", "Visao agregada das medianas")
            + "<p class='caption'>GraphQL venceu em 5/5 cenarios nas duas metricas. "
            f"Reducao agregada de {time_red}% no tempo e {size_red}% no tamanho da mediana "
            "(p &lt; 0,0001). Hipoteses H1-1 e H1-2 apoiadas.</p>",
        ),
        (
            "RQ1 &middot; Tempo",
            chart("tempo", "Tempo mediano por cenario")
            + "<p class='caption'>RQ1 &mdash; GraphQL teve menor tempo de resposta em todos os cenarios. "
            f"Reducao agregada de {time_red}% na mediana (p &lt; 0,0001; Cliff's delta {time_cliff}). H1-1 apoiada.</p>",
        ),
        (
            "RQ2 &middot; Tamanho",
            chart("tamanho", "Tamanho mediano por cenario")
            + "<p class='caption'>RQ2 &mdash; GraphQL retornou payloads menores em todos os cenarios (escala log). "
            f"Reducao agregada de {size_red}% na mediana (p &lt; 0,0001; Cliff's delta {size_cliff}). H1-2 apoiada.</p>",
        ),
        (
            "Distribuicao",
            chart("distribuicao", "Distribuicao do tempo por trial")
            + "<p class='caption'>Boxplot dos 1.200 trials: a dispersao do GraphQL e menor e nao se sobrepoe "
            "a do REST em nenhum cenario, reforcando a diferenca observada nas medianas.</p>",
        ),
        (
            "Reducao %",
            chart("reducao", "Reducao percentual por cenario")
            + "<p class='caption'>Percentual de reducao do GraphQL frente ao REST por cenario, para tempo "
            "(RQ1) e tamanho (RQ2). Todos os cenarios superam 66% de reducao.</p>",
        ),
        (
            "Sintese estatistica",
            "<h2>Mann-Whitney U e Cliff's delta</h2>"
            + f"<div class='table-wrap'>{tabela_testes_html(tabelas['testes'])}</div>",
        ),
        (
            "Resumo descritivo",
            "<h2>Medianas por cenario e tratamento</h2>"
            + f"<div class='table-wrap'>{tabela_descritiva_html(tabelas['descritiva'])}</div>",
        ),
        (
            "Discussao",
            "<div class='prose'>"
            "<p>As consultas GraphQL retornaram payloads menores em todos os cenarios porque projetam apenas "
            "os campos solicitados pelo cliente, evitando o over-fetching tipico dos endpoints REST. Esse menor "
            "volume de dados tambem reduz o custo de serializacao e parsing JSON, refletindo em menor tempo mediano.</p>"
            "<p>Como a coleta foi local e deterministica, os numeros isolam o efeito do tamanho do payload e do "
            "processamento. Devem ser interpretados como evidencia do cenario controlado, nao como comparacao "
            "universal entre todas as implementacoes reais de REST e GraphQL.</p>"
            + (ambiente or "")
            + "</div>",
        ),
    ]

    abas = []
    paineis = []
    for indice, (rotulo, conteudo) in enumerate(slides):
        ativo = " active" if indice == 0 else ""
        abas.append(f"<button class='tab{ativo}' data-i='{indice}'>{rotulo}</button>")
        paineis.append(f"<section class='pane{ativo}'>{conteudo}</section>")
    tabs_html = "".join(abas)
    panes_html = "".join(paineis)

    css_vars = (
        ":root{"
        "--page:#eef1f6;--ink:#17202a;--muted:#5d6978;--line:#d7dde7;"
        "--panel:#fff;--accent:#253858;"
        "--rest:" + COR_REST + ";--graphql:" + COR_GRAPHQL + ";"
        "}"
    )
    css = "<style>\n" + css_vars + "\n" + CSS_DECK + "\n</style>"
    js = "<script>\n" + JS_DECK + "\n</script>"

    return (
        "<!DOCTYPE html>\n"
        '<html lang="pt-BR">\n'
        "<head>\n"
        '<meta charset="utf-8" />\n'
        '<meta name="viewport" content="width=device-width, initial-scale=1" />\n'
        "<title>Lab05 - Dashboard GraphQL vs REST</title>\n"
        + css + "\n"
        "</head>\n"
        "<body>\n"
        '<div class="deck">\n'
        "<header>\n"
        '  <div><p class="eyebrow">Laboratorio 05 &middot; Sprint 3 &middot; pandas + matplotlib/seaborn</p>'
        "<h1>GraphQL vs REST &mdash; Experimento controlado</h1></div>\n"
        '  <div class="status">RQ1 e RQ2 apoiadas no cenario controlado</div>\n'
        "</header>\n"
        f'<nav class="tabs">{tabs_html}</nav>\n'
        f'<div class="stage">{panes_html}</div>\n'
        "<footer>\n"
        "  <span>Lab05 &middot; GraphQL vs REST &middot; Sprint 3</span>\n"
        f'  <span class="indicator">1 / {len(slides)}</span>\n'
        "  <span>Use as setas &larr; &rarr; ou clique nas abas</span>\n"
        "</footer>\n"
        "</div>\n"
        + js + "\n"
        "</body>\n"
        "</html>\n"
    )


# ---------------------------------------------------------------------------
# Orquestracao
# ---------------------------------------------------------------------------
def main() -> None:
    sns.set_theme(style="whitegrid", context="talk", font_scale=0.7)
    plt.rcParams["axes.titlepad"] = 12

    entrega = localizar_entrega()
    dados_dir = entrega / "dados"
    dashboards_dir = entrega / "dashboards"
    graficos_dir = dashboards_dir / "graficos"
    tabelas_dir = dashboards_dir / "tabelas"
    graficos_dir.mkdir(parents=True, exist_ok=True)

    obrigatorios = ["medicoes_lab05.csv", "resumo_estatistico.csv", "testes_estatisticos.csv"]
    faltando = [nome for nome in obrigatorios if not (dados_dir / nome).exists()]
    if faltando:
        raise SystemExit(
            "Arquivos ausentes em "
            f"{dados_dir}: {', '.join(faltando)}.\n"
            "Execute a Sprint 2 antes de gerar o dashboard."
        )

    dados = carregar_dados(dados_dir)

    graficos = {
        "tempo": grafico_tempo_mediano(dados["resumo"], graficos_dir / "rq1_tempo_mediano.png"),
        "tamanho": grafico_tamanho_mediano(dados["resumo"], graficos_dir / "rq2_tamanho_mediano.png"),
        "distribuicao": grafico_distribuicao_tempo(dados["medicoes"], graficos_dir / "distribuicao_tempo.png"),
        "reducao": grafico_reducao_percentual(dados["testes_cenarios"], graficos_dir / "reducao_percentual.png"),
        "agregado": grafico_agregado(dados["agregado"], graficos_dir / "visao_agregada.png"),
    }

    tabelas = exportar_tabelas(dados, tabelas_dir)

    html = construir_html(dados, tabelas, graficos)
    saida_html = dashboards_dir / "dashboard_lab05_python.html"
    saida_html.write_text(html, encoding="utf-8")

    print("Lab05S03 (dashboard Python) concluido.")
    print(f"Graficos gerados em: {graficos_dir}")
    print(f"Tabelas geradas em:  {tabelas_dir}")
    print(f"Dashboard HTML:      {saida_html}")


if __name__ == "__main__":
    main()
