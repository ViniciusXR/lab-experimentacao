using System;
using System.Collections.Generic;
using System.IO;
using Lab03S03.Analysis;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lab03S03.Report
{
    public static class ReportGenerator
    {
        public static void Generate(List<AnalysisResult> results, string outputPath, int totalPrs)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, results, totalPrs));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(outputPath);
        }

        private static void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text("Laboratório 03 — Caracterizando a Atividade de Code Review no GitHub")
                    .FontSize(15).Bold().FontColor(Colors.Orange.Darken2);

                col.Item().Text("Relatório Final · Lab03S03 · Análise, Visualização e Síntese")
                    .FontSize(11).SemiBold().FontColor(Colors.Blue.Darken2);

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Engenharia de Software  ·  Laboratório de Experimentação de Software  ·  6º Período  ·  PUC Minas")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    row.AutoItem().Text($"Gerado em: {System.DateTime.Now:dd/MM/yyyy}")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });

                col.Item().Text("Professor: Danilo de Quadros Maia Filho")
                    .FontSize(8.5f).FontColor(Colors.Grey.Darken2);

                col.Item().Text("Alunos: Sthel Felipe Torres e Vinicius Xavier Ramalho")
                    .FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);

                col.Item().LineHorizontal(2).LineColor(Colors.Orange.Medium);
            });
        }

        private static void ComposeContent(IContainer container, List<AnalysisResult> results, int totalPrs)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                // Sumário / Índice Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("Índice").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);

                    // Adicionamos um Row para separar o Título do número da página justificado
                    box.Item().PaddingTop(4).Row(row => { row.RelativeItem().Text("1. Introdução").SemiBold().FontSize(12); row.AutoItem().Text("Pag. 2").FontSize(11).FontColor(Colors.Grey.Darken2); });
                    box.Item().Text("   1.1 Contexto").FontSize(11);
                    box.Item().Text("   1.2 Definição do Problema").FontSize(11);
                    box.Item().Text("   1.3 Questões de Pesquisa e Hipóteses").FontSize(11);
                    box.Item().Text("   1.4 Objetivo Principal").FontSize(11);

                    box.Item().PaddingTop(2).Row(row => { row.RelativeItem().Text("2. Planejamento e execução do experimento").SemiBold().FontSize(12); row.AutoItem().Text("Pag. 3").FontSize(11).FontColor(Colors.Grey.Darken2); });
                    box.Item().Text("   2.1 Evidência visual do planejamento - Parte 2").FontSize(11);

                    box.Item().PaddingTop(2).Row(row => { row.RelativeItem().Text("3. Metodologia").SemiBold().FontSize(12); row.AutoItem().Text("Pag. 5").FontSize(11).FontColor(Colors.Grey.Darken2); });
                    box.Item().Text("   3.1 Criação do Dataset").FontSize(11);
                    box.Item().Text("   3.2 Teste Estatístico").FontSize(11);
                    box.Item().Text("   3.3 Sumarização dos Resultados").FontSize(11);

                    box.Item().PaddingTop(2).Row(row => { row.RelativeItem().Text("4. Resultados (RQs)").SemiBold().FontSize(12); row.AutoItem().Text("Pag. 6").FontSize(11).FontColor(Colors.Grey.Darken2); });

                    // Adiciona dinamicamente as RQs calculando as páginas (Página base 6 = RQ01, cada RQ pula 1 BreakPage)
                    int pPage = 6;
                    foreach (var res in results)
                    {
                        box.Item().Row(row => 
                        {
                            row.RelativeItem().Text($"   • {res.RQCode}: {res.Title}").FontSize(11);
                            row.AutoItem().Text($"Pag. {pPage}").FontSize(11).FontColor(Colors.Grey.Darken2); 
                        });
                        pPage++;
                    }

                    box.Item().PaddingTop(2).Row(row => { row.RelativeItem().Text("5. Discussão Geral e Conclusão").SemiBold().FontSize(12); row.AutoItem().Text($"Pag. {pPage}").FontSize(11).FontColor(Colors.Grey.Darken2); });
                });

                col.Item().PageBreak();

                // Intro Box
                col.Item().Border(1).BorderColor(Colors.Orange.Lighten3).Padding(10).Background(Colors.Orange.Lighten5).Column(box =>
                {
                    box.Item().Text("1. Introdução").Bold().FontSize(15).FontColor(Colors.Orange.Darken3);
                    box.Item().PaddingTop(4).Text("A revisão de código (code review) é uma etapa indispensável no desenvolvimento de software, destacando-se em metodologias ágeis e projetos de código aberto (open source). Seu propósito é auditar modificações antes de incorporá-las à base principal do projeto, minimizando a inserção de falhas e elevando a qualidade do produto final. No ecossistema do GitHub, essa dinâmica ocorre primordialmente através de Pull Requests (PRs). Neles, as submissões são debatidas e julgadas por revisores, resultando em sua integração (MERGED) ou rejeição (CLOSED), frequentemente com o auxílio de checagens automatizadas. O presente laboratório tem como finalidade realizar uma caracterização empírica dessa prática em repositórios de destaque no GitHub. Para isso, investiga-se de que maneira fatores como tamanho da alteração, tempo de duração, riqueza da descrição e nível de interação afetam o desfecho do PR e a quantidade de revisões exigidas, evidenciando o impacto dessas variáveis no esforço analítico e na chance de aprovação.").FontSize(12);

                    box.Item().PaddingTop(8).Text("1.1 Contexto").SemiBold().FontSize(13).FontColor(Colors.Orange.Darken2);
                    box.Item().PaddingTop(2).Text("No GitHub, o PR atua como a unidade central para a análise de code review, pois congrega tanto as alterações técnicas quanto o histórico social do processo. Ele funciona, por um lado, como um \"pacote de código\" que deve ser validado quanto à sua exatidão e compatibilidade. Por outro, age como um fórum de negociação que registra comentários, exigências de correção, aprovações e o diálogo entre os envolvidos com diferentes atribuições.").FontSize(12);
                    box.Item().PaddingTop(4).Text("A teoria e a prática indicam que os atributos do PR impactam diretamente a carga de trabalho da revisão e as chances de aceitação. Modificações extensas costumam exigir mais tempo de leitura e aumentam o risco de erros passarem despercebidos, enquanto envios menores tendem a ser assimilados rapidamente. O intervalo entre a abertura e o fechamento de um PR pode revelar tanto a sua complexidade técnica quanto a necessidade de muito retrabalho. Além disso, a qualidade da descrição dita o ritmo da revisão, pois esclarece motivações, limites, riscos e métodos de teste, facilitando ou travando o trabalho do revisor. Por fim, o volume de interações (como o número de participantes e comentários) aponta para processos mais colaborativos, rigorosos ou polêmicos, influenciando o sucesso da integração.").FontSize(12);
                    box.Item().PaddingTop(4).Text("Como os repositórios populares variam drasticamente em termos de linguagem, governança, políticas de aceite e nível de automação, é crucial validar essas hipóteses intuitivas de forma empírica e em larga escala. Dessa forma, caracterizar o code review através de dados extraídos de PRs possibilita a identificação objetiva de padrões, gerando insights valiosos para colaboradores e mantenedores de projetos.").FontSize(12);

                    box.Item().PaddingTop(8).Text("1.2 Definição do Problema").SemiBold().FontSize(13).FontColor(Colors.Orange.Darken2);
                    box.Item().PaddingTop(2).Text("O foco deste experimento é investigar a relação entre as características observáveis dos PRs revisados e o desfecho do processo de revisão. De forma específica, analisa-se como as variáveis de tamanho, duração da análise, detalhamento da descrição e engajamento social determinam se um PR será efetivamente incorporado (MERGED) ou encerrado sem aprovação (CLOSED).").FontSize(12);
                    box.Item().PaddingTop(4).Text("Para além do status final, o estudo também mede o impacto dessas mesmas características na intensidade da revisão, utilizando o total de revisões executadas no PR como métrica. Para garantir que os dados reflitam predominantemente o esforço humano — e para mitigar o viés de encerramentos automatizados —, o conjunto de dados foi filtrado para incluir exclusivamente PRs que possuam ao menos uma avaliação registrada e cujo tempo de vida (da abertura ao desfecho) ultrapasse uma hora.").FontSize(12);

                    box.Item().PaddingTop(8).Text("1.3 Questões de Pesquisa e Hipóteses").SemiBold().FontSize(13).FontColor(Colors.Orange.Darken2);
                    box.Item().PaddingTop(2).Text("Abaixo estão as perguntas que norteiam a pesquisa e as hipóteses informais elaboradas a partir das expectativas do comportamento em repositórios Java populares no GitHub.").FontSize(12);
                    box.Item().PaddingTop(4).Text("• RQ01 (Tamanho vs Status): Estima-se que PRs com um volume massivo de modificações tenham menores chances de aprovação (MERGED). Alterações muito grandes sobrecarregam o revisor cognitivamente, elevam a probabilidade de falhas e complicam a análise de impacto, o que geralmente resulta em mais exigências de refatoração ou descarte.").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ02 (Tempo vs Status): PRs que demoram longos períodos em análise tendem a ser finalizados como CLOSED. Prazos extensos indicam atritos no processo, fazendo com que o código fique obsoleto frente à branch principal (envelhecimento do PR).").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ03 (Descrição vs Status): Textos descritivos mais robustos (maior body length) aumentam a probabilidade de um PR ser MERGED. Explicações minuciosas clarificam a intenção da mudança e métodos de teste, mitigando as dúvidas dos revisores.").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ04 (Interações vs Status): PRs com altos índices de comentários e participantes tendem a ser CLOSED. O alto engajamento frequentemente sinaliza polêmicas, identificação de falhas ou longos debates sobre arquitetura e estilo, gerando um desgaste que pode culminar na rejeição.").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ05 (Tamanho vs Revisões): Existe uma correlação positiva entre o tamanho do PR e a quantidade de revisões. Submissões extensas naturalmente demandam inspeções mais longas e são mais suscetíveis a apontamentos em diferentes módulos do código.").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ06 (Tempo vs Revisões): Quanto mais revisões um PR sofre, maior o seu tempo total de tramitação. Cada novo ciclo exige leitura, debate, codificação de correções e reexecução de testes automatizados (CI), alongando o período.").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ07 (Descrição vs Revisões): Antecipa-se uma correlação negativa, em que descrições mais ricas resultam em menos revisões. Quando a motivação e a lógica da mudança são bem expostas desde o princípio, o revisor tem menos dúvidas estruturais, encurtando rodadas de feedback.").FontSize(11);
                    box.Item().PaddingTop(2).Text("• RQ08 (Interações vs Revisões): Prevê-se uma associação positiva entre as métricas de interação e a quantidade de revisões. Conversas e discussões densas geralmente geram a necessidade de ajustes complementares. Assim, o feedback resulta em novas validações formais.").FontSize(11);

                    box.Item().PaddingTop(8).Text("1.4 Objetivo Principal").SemiBold().FontSize(13).FontColor(Colors.Orange.Darken2);
                    box.Item().PaddingTop(2).Text($"Realizar uma caracterização empírica da dinâmica de code review em repositórios de grande circulação no GitHub. O intuito é mapear como as métricas relativas a tamanho, duração temporal, qualidade do texto descritivo e engajamento social estão atreladas ao desfecho do processo e à quantidade de revisões recebidas. O fim último da pesquisa é extrair evidências quantitativas consistentes através da modelagem estatística de {totalPrs} PRs.").FontSize(12);
                });

                col.Item().PageBreak();

                // Flowcharts (Box 2)
                col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Background(Colors.Blue.Lighten5).Column(box =>
                {
                    box.Item().Text("2. Planejamento e execução do experimento").Bold().FontSize(15).FontColor(Colors.Blue.Darken3);
                    box.Item().PaddingTop(4).Text("As figuras abaixo documentam o planejamento experimental (desenho inicial, etapas e organização da análise).").FontSize(12);

                    string flow1Path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(results[0].ChartPath)), "charts", "fluxograma_etapas_1_2.png");
                    if (File.Exists(flow1Path))
                    {
                        box.Item().PaddingTop(8).AlignCenter().Height(400).Image(flow1Path).FitArea();
                    }
                });

                col.Item().PageBreak();

                col.Item().Column(x =>
                {
                    x.Spacing(6);
                    x.Item().Text("2.1 Evidência visual do planejamento - Parte 2").SemiBold().FontSize(12);
                    string flow2Path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(results[0].ChartPath)), "charts", "fluxograma_etapas_3_4.png");
                    if (File.Exists(flow2Path))
                    {
                         x.Item().AlignCenter().Height(400).Image(flow2Path).FitArea();
                    }
                });

                col.Item().PageBreak();

                // Metodologia Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("3. Metodologia").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);

                    box.Item().PaddingTop(4).Text("3.1 Criação do Dataset").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text($"Os dados foram coletados via API GraphQL do GitHub. O dataset é composto pelos {totalPrs} PRs que satisfazem todos os critérios abaixo:").FontSize(12);
                    box.Item().PaddingTop(3).Text("• Repositórios: 200 repositórios mais populares do GitHub (ordenados por número de estrelas).").FontSize(11);
                    box.Item().Text("• Requisito mínimo por repositório: pelo menos 100 PRs com status MERGED ou CLOSED.").FontSize(11);
                    box.Item().Text("• Status do PR: apenas MERGED ou CLOSED (PRs abertos foram excluídos).").FontSize(11);
                    box.Item().Text("• Revisões: apenas PRs com pelo menos uma revisão registrada (campo reviews.totalCount ≥ 1).").FontSize(11);
                    box.Item().Text("• Filtro anti-bot: apenas PRs cujo intervalo entre a criação e o fechamento/merge é superior a 1 hora.").FontSize(11);

                    box.Item().PaddingTop(8).Text("3.2 Teste Estatístico").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text("Utilizamos o coeficiente de correlação de Spearman para todas as análises. A escolha é justificada pela natureza não-paramétrica dos dados: as métricas coletadas apresentam assimetria severa e outliers extremos, tornando a distribuição normal inviável e o teste de Pearson inadequado. O Spearman opera sobre postos (ranks), sendo robusto a essas distorções. Calculamos também o p-valor (aproximação t-Student bilateral) com limiar de significância α = 0,05.").FontSize(12);

                    box.Item().PaddingTop(8).Text("3.3 Sumarização dos Resultados").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text("Para a Dimensão A (Status do PR), a variável dependente é binária (MERGED = 1, CLOSED = 0). A agregação dessas métricas é baseada nas correlações da população estatística gerada pelo Dataset completo de repositórios.").FontSize(12);
                });

                // Resultados — título da seção
                col.Item().PaddingTop(4).Text("4. Resultados (RQs)").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);

                // Cada RQ em sua própria caixa para permitir quebras de página limpas e gráficos maiores
                foreach (var res in results)
                {
                    col.Item().PageBreak();
                    col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Column(box =>
                    {
                        box.Item().Text($"{res.RQCode} — {res.Title}").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                        box.Item().PaddingTop(2).Text($"Hipótese: {res.Hypothesis}").Italic().FontSize(12).FontColor(Colors.Grey.Darken2);

                        box.Item().PaddingVertical(5).Element(c => ComposeMedianTable(c, res));

                        if (!string.IsNullOrEmpty(res.ChartPath) && File.Exists(res.ChartPath))
                        {
                            box.Item().PaddingVertical(8).AlignCenter().Image(res.ChartPath).FitArea();
                        }

                        string pValLabel = res.PValue < 0.001 ? "< 0,001" : $"= {res.PValue:e3}";
                        box.Item().PaddingTop(10).Text($"Correlação [{res.PrimaryLabel}]: {res.Interpretation} (ρ = {res.SpearmanRho:F3}, p {pValLabel}{(res.PValue > 0.05 ? " — sem significância estatística" : "")})").FontSize(12).SemiBold();

                        if (res.HasSecondaryMetric)
                        {
                            string secPValLabel = res.SecondaryPValue < 0.001 ? "< 0,001" : $"= {res.SecondaryPValue:e3}";
                            box.Item().PaddingTop(2).Text($"  ↳ [{res.SecondaryLabel}]: {res.SecondaryInterpretation} (ρ = {res.SecondarySpearmanRho:F3}, p {secPValLabel}{(res.SecondaryPValue > 0.05 ? " — sem significância estatística" : "")})").FontSize(11).FontColor(Colors.Grey.Darken1);
                        }

                        box.Item().PaddingTop(8).Text($"Discussão: {res.Discussion}").FontSize(12);
                    });
                }

                col.Item().PageBreak();
                // Conclusion Box
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(box =>
                {
                    box.Item().Text("5. Discussão Geral e Conclusão").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);
                    box.Item().PaddingTop(4).Text("Analisando as 8 Questões de Pesquisa, observamos que 5 de 8 hipóteses foram confirmadas (RQ02, RQ05, RQ06, RQ07, RQ08) e apenas 3 foram refutadas (RQ01, RQ03, RQ04). Dentre os achados confirmados, o relacionamento de interações dita positivamente a cadência e número de revisões com a maior força do relatório (RQ08). Curiosamente, outras hipóteses formuladas apresentaram resultados inesperados: a RQ01 surpreendeu ao mostrar levemente que PRs MAIORES tendem a ser aceitos; a RQ03 não apresentou significância estatística (p-valor > 0,05), evidenciando que o comprimento da descrição não influencia diretamente na aprovação; e a RQ04, embora estatisticamente significativa (p-valor < 0,05), revelou uma correlação negativa, indicando que um maior volume de comentários está associado à rejeição do PR, possivelmente devido a discussões extensas geradas por códigos problemáticos antes de seu fechamento definitivo.").FontSize(12);
                    box.Item().PaddingTop(8).Text("Limitações do estudo e Trabalhos Futuros:").SemiBold().FontSize(13);
                    box.Item().PaddingTop(2).Text("Observa-se que em variáveis como 'Review Count' a mediana massivamente se assenta em torno de 1,00, apontando que variabilidades altas acontecem fundamentalmente em PRs contendo grandes distorções, com as correlações em geral não ultrapassando a limitação de tendências fracas e moderadas. Para trabalhos futuros, sugere-se analisar isoladamente apenas PRs com review_count ≥ 2 para verificar se os padrões se mantêm ou fortalecem em revisões mais elaboradas.").FontSize(12);
                });
            });
        }

        private static void ComposeMedianTable(IContainer container, AnalysisResult r)
        {
            bool isStatusVsMetric = r.RQCode == "RQ01" || r.RQCode == "RQ02" || r.RQCode == "RQ03" || r.RQCode == "RQ04";
            string primaryLabel = string.IsNullOrEmpty(r.PrimaryLabel) ? r.Title : r.PrimaryLabel;

            container.Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Métrica").Bold();
                    if (isStatusVsMetric)
                    {
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana MERGED").Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana CLOSED").Bold();
                    }
                    else
                    {
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Med. Variável Indep.").Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(2).Text("Mediana Revisões").Bold();
                    }
                });

                // Linha da métrica primária
                table.Cell().Padding(2).Text(primaryLabel);
                table.Cell().Padding(2).Text(r.MedianMerged.ToString("F2"));
                table.Cell().Padding(2).Text(r.MedianClosed.ToString("F2"));

                // Linha da métrica secundária (Tamanho: total de linhas; Interações: participantes)
                if (r.HasSecondaryMetric)
                {
                    table.Cell().Padding(2).Text(r.SecondaryLabel).FontColor(Colors.Grey.Darken2);
                    table.Cell().Padding(2).Text(r.SecondaryMedianMerged.ToString("F2")).FontColor(Colors.Grey.Darken2);
                    table.Cell().Padding(2).Text(r.SecondaryMedianClosed.ToString("F2")).FontColor(Colors.Grey.Darken2);
                }
            });
        }
    }
}