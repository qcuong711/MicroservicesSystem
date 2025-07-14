"use client";

import { useSession, signIn, signOut } from "next-auth/react";

export default function LoginButton() {
  const { data: session } = useSession();

  const buttonStyle =
    "bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded";

  if (session) {
    return (
      <div className="text-center">
        <p>Signed in as {session.user?.email}</p>
        <button className={buttonStyle} onClick={() => signOut()}>
          Sign out
        </button>
      </div>
    );
  }

  return (
    <div className="text-center">
      <p>Not signed in</p>
      <button className={buttonStyle} onClick={() => signIn("keycloak")}>
        Sign in
      </button>
    </div>
  );
} 