import { redirect } from "next/navigation";
import { getSessionProfile } from "@/lib/server-auth";

export default async function Home() {
  redirect((await getSessionProfile()) ? "/dashboard" : "/login");
}
