# PDFtoBook

Leitor pessoal de PDFs para Android, feito em Unity 6.3 LTS. Os arquivos e o progresso ficam apenas no aparelho.

## Primeira versão

- Biblioteca com importação pelo seletor do Android, busca e exclusão da cópia local.
- Leitura de uma página por vez, botões, deslize horizontal e toque nas laterais.
- Zoom com dois dedos, arraste e botão Ajustar.
- Navegação direta por número de página e barra de progresso.
- Marcadores por livro e retomada automática da última página exibida.
- Tema do fundo de leitura claro/escuro; o conteúdo do PDF mantém suas cores originais.

## Desenvolvimento

Abra o projeto com Unity `6000.3.13f1`, com Android Build Support, SDK, NDK e OpenJDK instalados. A cena principal é `Assets/Scenes/Reader.unity`. A interface usa uGUI, TextMeshPro e Input System.

O menu `PDFtoBook/Create reader scene` recria a cena inicial a partir do construtor em C#. Use somente para reconstruir a interface base; alterações manuais feitas na cena serão substituídas. `PDFtoBook/Configure Android` aplica os parâmetros de compilação. A cena original SampleScene é preservada.

A importação e a renderização usam APIs nativas Android e funcionam no APK. No Editor Windows é possível visualizar e editar a interface; não há renderizador de PDF para Windows nesta versão.

O identificador do aplicativo é `com.pdftobook.reader`. O APK inicial usa IL2CPP/ARM64, Android mínimo API 25 e alvo API 36, orientação retrato e OpenGLES3. As primeiras compilações usam a assinatura de desenvolvimento padrão da Unity; configure uma chave própria antes de distribuir uma versão definitiva e preserve a chave para futuras atualizações.

## Organização

- `Assets/PDFtoBook/LibraryStore.cs`: registros, progresso, marcadores e JSON versionado.
- `Assets/PDFtoBook/AndroidPdf.cs`: chamadas C# para a ponte Java.
- `Assets/Plugins/Android/PdfBridge.java`: fila de renderização e ciclo de vida do PdfRenderer.
- `Assets/Plugins/Android/PdfImportActivity.java`: seletor de PDF, cópia local e validação do documento.
- `Assets/PDFtoBook/ReaderApp.cs`: navegação, persistência e cache das páginas.
- `Assets/PDFtoBook/ReaderView.cs`: referências e componentes da interface.
- `Assets/PDFtoBook/PageGestures.cs`: toques, deslizes e zoom.
- `Assets/Editor/ReaderProjectSetup.cs`: criação da cena e configurações Android.
- `Assets/Editor/ReaderBuild.cs`: compilação pelo Editor com resultado em JSON.

## Dados e privacidade

O seletor usa `ACTION_OPEN_DOCUMENT`, MIME `application/pdf` e `EXTRA_LOCAL_ONLY`. O aplicativo copia o PDF para `Context.getNoBackupFilesDir()/reader` e pode continuar lendo mesmo que o original seja movido. A exclusão na biblioteca não altera o original.

O manifesto remove `INTERNET` e `ACCESS_NETWORK_STATE`, desativa backup em nuvem e não solicita acesso amplo ao armazenamento. Os dados ficam no diretório excluído de backup do Android. A integração Pipeline usada no Editor é desativada nos builds do aplicativo. A ausência das permissões deve ser conferida no APK gerado.

O índice local é gravado em arquivo temporário, sincronizado e substituído com uma cópia de segurança da versão anterior. O progresso é salvo a cada página efetivamente exibida e ao pausar. Desinstalar o aplicativo ou limpar seus dados remove a biblioteca e o progresso. Ainda não existe exportação manual de backup.

## Renderização e limites

O PdfRenderer do sistema Android abre apenas uma página por vez, em um executor serial fora da interface. Cada bitmap é liberado após a transferência. Arquivos PNG temporários são removidos após a carga da textura. O cache Unity mantém no máximo três páginas, com orçamento de 48 MiB de texturas, mais os buffers transitórios necessários à renderização. A próxima página é antecipada e requisições antigas não substituem a página escolhida.

A resolução inicial tem lado maior de 1920 pixels; ao finalizar o zoom, a página pode ser renderizada a até 2560 pixels. PDFs preservam seu layout fixo. Não há reflow, extração/pesquisa de texto no documento, OCR ou suporte a senha nesta versão.

## Validação

Os arquivos locais de QA e o APK ficam em `Builds/`, ignorado pelo Git. O PDF sintético de 500 páginas contém uma página horizontal e outra com formato menor para verificar proporção, navegação e retomada. Ele não faz parte da biblioteca distribuída com o aplicativo.

Validação inicial concluída no Xiaomi com Android 16: importação de um PDF real de 500 páginas e retomada na página 60 após fechar e reabrir o aplicativo, confirmadas pelo usuário. Também passaram a compilação Android, a verificação de assinatura/permissões, os testes de persistência e recuperação do JSON e a pinça com toques sintéticos no Input System.

## Identidade visual

A logo original fica em `Assets/Branding/PDFtoBookLogo.png`; ela é preservada sem alterações. O menu `PDFtoBook/Apply logo and green palette` reaplica a identidade visual na cena atual e gera os tamanhos dos ícones Android com amostragem por vizinho mais próximo.

A interface usa fundo preto e texto neutro branco. Botões positivos usam `#61A53F`; voltar, cancelar, fechar e excluir usam `#477238`; ações neutras usam branco. Os textos dentro dos botões recebem contraste apropriado ao preenchimento.

A fonte é `Not Jam Signature 21 SDF`, gerada a partir do TTF fornecido, com atlas/material próprios e arquivo de origem em `Assets/Branding/Fonts`. O conjunto contém os acentos usados em português. Os sprites ficam em `Assets/Branding/UI`: `UI_Button.png` usa bordas de 3 pixels em cada lado, e `UI_ButtonSlider.png` usa 7 pixels. As imagens são do tipo Sliced, sem mipmaps, sem compressão e com filtro Point, preservando os cantos ao redimensionar.
