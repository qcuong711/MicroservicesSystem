import { auth } from "@/auth";
import LoginButton from "@/components/LoginButton";
import { Button } from "@/components/ui/button";
import Link from "next/link";

export default async function Home() {
  const session = await auth();

  return (
    <main className="flex flex-col items-center justify-center min-h-[calc(100vh-80px)]">
      <div className="text-center">
        <h1 className="text-4xl md:text-6xl font-bold tracking-tighter mb-4">
          Welcome to the School Management Portal
        </h1>
        <p className="max-w-[600px] text-lg text-muted-foreground mx-auto mb-8">
          A centralized platform to manage students, internships, and more.
          Streamline your administrative tasks with ease.
        </p>
        {!session ? (
          <LoginButton />
        ) : (
          <div className="flex gap-4 justify-center">
            <Button asChild>
              <Link href="/students">Manage Students</Link>
            </Button>
            <Button variant="outline" asChild>
              <Link href="/internships">Manage Internships</Link>
            </Button>
          </div>
        )}
      </div>
    </main>
  );
} 