interface InlineErrorProps {
  message: string | null;
}

export const InlineError = ({ message }: InlineErrorProps) => (
  <div className="inline-error-slot">
    {message && (
      <div className="inline-error" role="alert">
        {message}
      </div>
    )}
  </div>
);
