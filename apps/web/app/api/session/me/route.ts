import { NextResponse } from "next/server";
import { getSessionProfile } from "@/lib/server-auth";

export async function GET() {
  const profile = await getSessionProfile();
  return profile ? NextResponse.json(profile) : NextResponse.json({ title: "Unauthorized" }, { status: 401 });
}
