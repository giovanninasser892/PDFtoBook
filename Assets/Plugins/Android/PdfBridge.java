package com.pdftobook.reader;

import android.app.Activity;
import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.pdf.PdfRenderer;
import android.os.ParcelFileDescriptor;
import com.unity3d.player.UnityPlayer;
import org.json.JSONObject;
import java.io.File;
import java.io.FileOutputStream;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/** All PDF operations are serialized off the UI/Unity threads. No network APIs. */
public final class PdfBridge {
    static final ExecutorService WORKER = Executors.newSingleThreadExecutor();
    private static String receiver;
    private static Context context;
    private static PdfRenderer renderer;
    private static String documentSession;

    public static String initialize(String objectName) {
        receiver = objectName;
        context = UnityPlayer.currentActivity.getApplicationContext();
        File root = new File(context.getNoBackupFilesDir(), "reader");
        if (!root.isDirectory() && !root.mkdirs()) throw new IllegalStateException("Não foi possível criar a biblioteca.");
        File[] partial = root.listFiles((dir, name) -> name.endsWith(".part"));
        if (partial != null) for (File file : partial) file.delete();
        File[] old = context.getCacheDir().listFiles((dir, name) -> name.startsWith("pdfpage-"));
        if (old != null) for (File file : old) file.delete();
        return root.getAbsolutePath();
    }

    static JSONObject message(String type, String request) {
        JSONObject value = new JSONObject();
        try { value.put("type", type); value.put("request", request); } catch (Exception ignored) { }
        return value;
    }

    static void send(JSONObject value) {
        UnityPlayer.UnitySendMessage(receiver, "OnNativeEvent", value.toString());
    }

    static void failure(String request, Exception error) {
        JSONObject value = message("error", request);
        String description = "Não foi possível ler este PDF. O arquivo pode estar danificado.";
        if (error instanceof SecurityException) description = "PDF protegido por senha ou acesso ao arquivo não permitido.";
        if (error instanceof java.io.FileNotFoundException) description = "O arquivo não foi encontrado. Importe o PDF novamente.";
        try { value.put("error", description); } catch (Exception ignored) { }
        send(value);
    }

    public static void pickDocument() {
        Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            try { activity.startActivity(new android.content.Intent(activity, PdfImportActivity.class)); }
            catch (Exception error) { failure("import", error); }
        });
    }

    public static void openDocument(String path, String session) {
        WORKER.execute(() -> {
            try {
                closeInternal();
                File file = new File(path);
                File root = new File(context.getNoBackupFilesDir(), "reader");
                if (!file.getCanonicalPath().startsWith(root.getCanonicalPath() + File.separator)) throw new SecurityException();
                ParcelFileDescriptor descriptor = ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
                try { renderer = new PdfRenderer(descriptor); }
                catch (Exception error) { descriptor.close(); throw error; }
                documentSession = session;
                JSONObject value = message("opened", session);
                value.put("pageCount", renderer.getPageCount());
                send(value);
            } catch (Exception error) { failure(session, error); }
        });
    }

    public static void renderPage(int pageIndex, int longEdge, String request, String session) {
        WORKER.execute(() -> {
            Bitmap bitmap = null;
            File output = null;
            try {
                if (renderer == null || !session.equals(documentSession)) return;
                int width, height;
                try (PdfRenderer.Page page = renderer.openPage(pageIndex)) {
                    float scale = (float)Math.max(256, Math.min(longEdge, 2560)) / Math.max(page.getWidth(), page.getHeight());
                    width = Math.max(1, Math.round(page.getWidth() * scale));
                    height = Math.max(1, Math.round(page.getHeight() * scale));
                    bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
                    bitmap.eraseColor(Color.WHITE);
                    page.render(bitmap, null, null, PdfRenderer.Page.RENDER_MODE_FOR_DISPLAY);
                }
                output = File.createTempFile("pdfpage-", ".png", context.getCacheDir());
                try (FileOutputStream stream = new FileOutputStream(output)) {
                    if (!bitmap.compress(Bitmap.CompressFormat.PNG, 100, stream)) throw new java.io.IOException();
                }
                JSONObject value = message("page", request);
                value.put("session", session);
                value.put("page", pageIndex);
                value.put("path", output.getAbsolutePath());
                send(value);
            } catch (Exception error) {
                if (output != null) output.delete();
                failure(request, error);
            } finally { if (bitmap != null) bitmap.recycle(); }
        });
    }

    private static void closeInternal() {
        if (renderer != null) { renderer.close(); renderer = null; }
        documentSession = null;
    }

    public static void closeDocument() { WORKER.execute(PdfBridge::closeInternal); }
}