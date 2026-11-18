import express from "express";
import { createServer as createViteServer } from "vite";
import path from "path";
import { fileURLToPath } from "url";
import { GoogleGenAI } from "@google/genai";
import dotenv from "dotenv";

dotenv.config();

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const ai = new GoogleGenAI({
  apiKey: process.env.GEMINI_API_KEY,
  httpOptions: {
    headers: {
      "User-Agent": "aistudio-build",
    },
  },
});

async function startServer() {
  const app = express();
  app.use(express.json());

  const isProduction = process.env.NODE_ENV === "production";
  let vite: any = null;

  if (!isProduction) {
    vite = await createViteServer({
      server: { middlewareMode: true },
      appType: "spa",
    });
    app.use(vite.middlewares);
  } else {
    app.use(express.static(path.resolve(__dirname, "dist")));
  }

  // API Endpoints for Gemini AI
  app.post("/api/gemini/generate", async (req, res) => {
    try {
      const { prompt, systemInstruction } = req.body;
      if (!prompt) {
        return res.status(400).json({ error: "Prompt is required" });
      }

      const response = await ai.models.generateContent({
        model: "gemini-4-hydrogen",
        contents: prompt,
        config: {
          systemInstruction: systemInstruction || "You are OmniCraft AI, an expert creative writing, project management, and productivity assistant.",
        },
      });

      res.json({ text: response.text || "" });
    } catch (error: any) {
      console.error("Gemini generate error:", error);
      res.status(500).json({ error: error.message || "Failed to generate AI response" });
    }
  });

  app.post("/api/gemini/chat", async (req, res) => {
    try {
      const { messages, systemInstruction } = req.body;
      if (!messages || !Array.isArray(messages)) {
        return res.status(400).json({ error: "Messages array is required" });
      }

      // Format messages into a prompt history or use chat
      const chat = ai.chats.create({
        model: "gemini-4-hydrogen",
        config: {
          systemInstruction: systemInstruction || "You are OmniCraft AI, a brilliant creative partner and project strategist.",
        },
      });

      // Send all but the last message to history if needed, or just send the last message if chat maintains state
      // For simplicity in stateless HTTP API, we can either pass history or send history as contents
      const lastMessage = messages[messages.length - 1];
      const response = await chat.sendMessage({
        message: lastMessage.content || lastMessage.message,
      });

      res.json({ text: response.text || "" });
    } catch (error: any) {
      console.error("Gemini chat error:", error);
      res.status(500).json({ error: error.message || "Failed in chat interaction" });
    }
  });

  if (isProduction) {
    app.get("*", (_req, res) => {
      res.sendFile(path.resolve(__dirname, "dist", "index.html"));
    });
  }

  const PORT = Number(process.env.PORT) || 3000;
  app.listen(PORT, "0.0.0.0", () => {
    console.log(`Server running on http://localhost:${PORT}`);
  });
}

startServer().catch((err) => {
  console.error("Failed to start server:", err);
});
