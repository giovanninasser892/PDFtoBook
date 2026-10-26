package com.pdftobook.reader;

import android.app.Activity;
import android.content.Intent;
import android.database.Cursor;
import android.graphics.pdf.PdfRenderer;
import android.net.Uri;
import android.os.Bundle;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;
import org.json.JSONObject;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.util.UUID;

/** Dedicated picker Activity avoids replacing Unity's Activity or requesting storage permissions. */
public final class PdfImportActivity extends Activity {
    @Override public void onCreate(Bundle saved) {
        super.onCreate(saved);
        if (saved != null) return;
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("application/pdf");
        intent.putExtra(Intent.EXTRA_LOCAL_ONLY, true);
        try { startActivityForResult(intent, 71); }
        catch (Exception error) { PdfBridge.failure("import", error); finish(); }
    }

    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != 71) return;
        if (result != RESULT_OK || data == null || data.getData() == null) {
            PdfBridge.send(PdfBridge.message("cancelled", "import"));
            finish(); return;
        }
        Uri uri = data.getData();
        // Keep this Activity alive until its temporary URI grant has been consumed.
        PdfBridge.WORKER.execute(() -> {
            File temporary = null;
            File destination = null;
            try {
                String title = "Documento";
                try (Cursor cursor = getContentResolver().query(uri, new String[]{OpenableColumns.DISPLAY_NAME}, null, null, null)) {
                    if (cursor != null && cursor.moveToFirst()) title = cursor.getString(0);
                }
                if (title == null || title.trim().isEmpty()) title = "Documento";
                title = title.replaceFirst("(?i)\\.pdf$", "");
                if (title.length() > 160) title = title.substring(0, 160);
                String id = UUID.randomUUID().toString();
                File root = new File(getNoBackupFilesDir(), "reader");
                temporary = new File(root, id + ".part");
                destination = new File(root, id + ".pdf");
                try (InputStream input = getContentResolver().openInputStream(uri);
                     FileOutputStream output = new FileOutputStream(temporary)) {
                    if (input == null) throw new java.io.FileNotFoundException();
                    byte[] buffer = new byte[65536];
                    int length;
                    while ((length = input.read(buffer)) != -1) output.write(buffer, 0, length);
                    output.getFD().sync();
                }
                int count;
                ParcelFileDescriptor descriptor = ParcelFileDescriptor.open(temporary, ParcelFileDescriptor.MODE_READ_ONLY);
                PdfRenderer check;
                try { check = new PdfRenderer(descriptor); }
                catch (Exception error) { descriptor.close(); throw error; }
                try { count = check.getPageCount(); } finally { check.close(); }
                if (count < 1 || !temporary.renameTo(destination)) throw new java.io.IOException();
                JSONObject value = PdfBridge.message("imported", "import");
                value.put("id", id); value.put("title", title); value.put("pageCount", count);
                PdfBridge.send(value);
            } catch (Exception error) {
                if (temporary != null) temporary.delete();
                if (destination != null) destination.delete();
                PdfBridge.failure("import", error);
            } finally { runOnUiThread(this::finish); }
        });
    }
}