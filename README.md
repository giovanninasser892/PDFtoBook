# PDFtoBook

Leitor pessoal de PDFs para Android, feito em Unity 6.3 LTS. Os arquivos e o progresso ficam apenas no aparelho.

## Funcionalidades

- Biblioteca com importação pelo seletor do Android, busca e exclusão da cópia local.
- Leitura de uma página por vez, botões, deslize horizontal e toque nas laterais.
- Zoom com dois dedos, arraste e botão Ajustar.
- Navegação direta por número de página e barra de progresso.
- Marcadores de páginas por livro e retomada automática da última página exibida.
- Missão diária com meta configurável, contador de avanços e reinício às 00:00 do horário local.
- Pin de posição no texto, preservado ao fechar o livro/app e removido ao trocar de página.
- Tema do fundo de leitura claro/escuro; o conteúdo do PDF mantém suas cores originais.

## Desenvolvimento

Abra o projeto com Unity `6000.3.13f1`, com Android Build Support, SDK, NDK e OpenJDK instalados. A cena principal é `Assets/Scenes/Reader.unity`. A interface usa uGUI, TextMeshPro e Input System.

O menu `PDFtoBook/Create reader scene` recria a cena inicial a partir do construtor em C#. Use somente para reconstruir a interface base; alterações manuais feitas na cena serão substituídas. `PDFtoBook/Configure Android` aplica os parâmetros de compilação. A cena original SampleScene é preservada.

A importação e a renderização usam APIs nativas Android e funcionam no APK. No Editor Windows é possível visualizar e editar a interface; não há renderizador de PDF para Windows nesta versão.

O identificador do aplicativo é `com.pdftobook.reader`. O APK inicial usa IL2CPP/ARM64, Android mínimo API 25 e alvo API 36, orientação retrato e OpenGLES3. As primeiras compilações usam a assinatura de desenvolvimento padrão da Unity; configure uma chave própria antes de distribuir uma versão definitiva e preserve a chave para futuras atualizações.

## Missão diária e pin

Na biblioteca, toque em **Meta** e escolha de 1 a 9999 páginas por dia. A meta vale para todos os livros; alterar a meta mantém a contagem do dia. Cada transição exibida para uma página de número maior soma 1, inclusive saltos por número, barra ou marcador. Voltar, reabrir, antecipar páginas no cache e renderizar o zoom não somam. Ler novamente uma página após voltar conta como um novo avanço. O contador pode ultrapassar a meta.

O contador reinicia à meia-noite segundo a data local do aparelho, mantendo o objetivo escolhido. Com o app fechado, essa atualização acontece ao reabrir; também é verificada ao retomar o app e durante o uso. O recurso funciona sem conexão e acompanha mudanças no relógio/data do aparelho.

Na leitura, toque em **Pin** para ativar a camada azul e depois toque no ponto desejado do PDF. **Mover pin** reposiciona o mesmo marcador; **Cancelar** mantém a posição anterior. Há apenas um pin por livro, vinculado à página atual. Ele usa coordenadas proporcionais ao PDF para acompanhar zoom e escala. Fechar o livro/app preserva o pin; solicitar outra página o remove imediatamente, mesmo que se volte depois. Os marcadores de páginas anteriores continuam separados em **Marcar/Marcadores**.

Os campos novos são adicionados ao JSON existente sem apagar livros, páginas ou marcadores antigos.

## Animação e feedback

A apresentação usa DOTween e Feel importados pelo usuário. O popup de avanço usa `Ease.OutBack`, sobe e desaparece; avanços rápidos agrupam o número visual sem alterar a contagem salva. O Feel usa `MMProgressBar` para o preenchimento da missão na biblioteca e na leitura, além de dois `MMF_Player` com feedbacks de escala para barras e contadores. A conclusão tem um aviso maior e um pulso mais forte, disparados uma vez por dia; essa informação também é salva no JSON.

Os botões comprimem ao pressionar e retornam com um pequeno bounce, inclusive os criados na biblioteca e em diálogos. As telas entram com fade e deslocamento, bloqueando toques durante a transição. Os efeitos usam tempo não escalado. O botão Excluir e a confirmação Remover usam `#B9451D`, com texto branco.

A cena usa Canvas Screen Space Camera e pós-processamento URP. O perfil `Assets/Settings/ReaderPostProcessing.asset` mantém bloom sutil: intensidade 0,06, threshold 0,95, scatter 0,12 e duas iterações. O tonemapping fica desativado para preservar o branco do PDF. Os efeitos podem ser ajustados no `ReaderFeedbacks`, nos dois players Feel e no perfil de Volume. `ReaderMotionSetup.Apply()` atualiza esses elementos na cena atual sem recriar a biblioteca ou o leitor.

## Organização

- `Assets/PDFtoBook/LibraryStore.cs`: registros, progresso, marcadores e JSON versionado.
- `Assets/PDFtoBook/DailyMission.cs`: data local, contagem de avanços e meta diária.
- `Assets/PDFtoBook/AndroidPdf.cs`: chamadas C# para a ponte Java.
- `Assets/Plugins/Android/PdfBridge.java`: fila de renderização e ciclo de vida do PdfRenderer.
- `Assets/Plugins/Android/PdfImportActivity.java`: seletor de PDF, cópia local e validação do documento.
- `Assets/PDFtoBook/ReaderApp.cs`: navegação, persistência e cache das páginas.
- `Assets/PDFtoBook/ReaderView.cs`: referências e componentes da interface.
- `Assets/PDFtoBook/PageGestures.cs`: toques, deslizes e zoom.
- `Assets/Editor/ReaderProjectSetup.cs`: criação da cena e configurações Android.
- `Assets/Editor/ReaderFeatureSetup.cs`: atualização pontual da cena com missão, pin e nova fonte.
- `Assets/PDFtoBook/ReaderFeedbacks.cs`: popup, conclusão e transições com DOTween/Feel.
- `Assets/PDFtoBook/ReaderButtonFeedback.cs`: animação de toque e submit dos botões.
- `Assets/Editor/ReaderMotionSetup.cs`: configuração pontual dos efeitos e do perfil URP.
- `Assets/Editor/ReaderBuild.cs`: compilação pelo Editor com resultado em JSON.

## Dados e privacidade

O seletor usa `ACTION_OPEN_DOCUMENT`, MIME `application/pdf` e `EXTRA_LOCAL_ONLY`. O aplicativo copia o PDF para `Context.getNoBackupFilesDir()/reader` e pode continuar lendo mesmo que o original seja movido. A exclusão na biblioteca não altera o original.

O manifesto remove `INTERNET`, `ACCESS_NETWORK_STATE` e a permissão `VIBRATE` trazida pelo Feel (os efeitos usados são visuais), desativa backup em nuvem e não solicita acesso amplo ao armazenamento. Os dados ficam no diretório excluído de backup do Android. A integração Pipeline usada no Editor é desativada nos builds do aplicativo. A ausência das permissões deve ser conferida no APK gerado.

O índice local é gravado em arquivo temporário, sincronizado e substituído com uma cópia de segurança da versão anterior. O progresso é salvo a cada página efetivamente exibida e ao pausar. Desinstalar o aplicativo ou limpar seus dados remove a biblioteca e o progresso. Ainda não existe exportação manual de backup.

## Renderização e limites

O PdfRenderer do sistema Android abre apenas uma página por vez, em um executor serial fora da interface. Cada bitmap é liberado após a transferência. Arquivos PNG temporários são removidos após a carga da textura. O cache Unity mantém no máximo três páginas, com orçamento de 48 MiB de texturas, mais os buffers transitórios necessários à renderização. A próxima página é antecipada e requisições antigas não substituem a página escolhida.

A resolução inicial tem lado maior de 1920 pixels; ao finalizar o zoom, a página pode ser renderizada a até 2560 pixels. PDFs preservam seu layout fixo. Não há reflow, extração/pesquisa de texto no documento, OCR ou suporte a senha nesta versão.

## Validação

Os arquivos locais de QA e o APK ficam em `Builds/`, ignorado pelo Git. O PDF sintético de 500 páginas contém uma página horizontal e outra com formato menor para verificar proporção, navegação e retomada. Ele não faz parte da biblioteca distribuída com o aplicativo.

Validação inicial concluída no Xiaomi com Android 16: importação de um PDF real de 500 páginas e retomada na página 60 após fechar e reabrir o aplicativo, confirmadas pelo usuário. Também passaram a compilação Android, a verificação de assinatura/permissões, os testes de persistência e recuperação do JSON e a pinça com toques sintéticos no Input System.

## Identidade visual

A logo original fica em `Assets/Branding/PDFtoBookLogo.png`; ela é preservada sem alterações. O menu `PDFtoBook/Apply logo and green palette` reaplica a identidade visual na cena atual e gera os tamanhos dos ícones Android com amostragem por vizinho mais próximo.

A interface usa fundo preto e texto neutro branco. Botões positivos usam `#61A53F`; voltar, cancelar e fechar usam `#477238`; excluir/remover usam `#B9451D`; ações neutras usam branco. Os textos dentro dos botões recebem contraste apropriado ao preenchimento.

A fonte é `Not Jam UI 15 SDF`, gerada a partir do TTF fornecido, com atlas/material próprios e arquivo de origem em `Assets/Branding/Fonts`. O conjunto contém os acentos usados em português. A logo foi removida da tela inicial; continua sendo usada no ícone Android. O título da biblioteca usa tamanho maior e negrito. O sprite `UI_Pin.png` é importado com filtro Point, sem compressão ou mipmaps. Os sprites ficam em `Assets/Branding/UI`: `UI_Button.png` usa bordas de 3 pixels em cada lado, e `UI_ButtonSlider.png` usa 7 pixels. As imagens são do tipo Sliced, sem mipmaps, sem compressão e com filtro Point, preservando os cantos ao redimensionar.
